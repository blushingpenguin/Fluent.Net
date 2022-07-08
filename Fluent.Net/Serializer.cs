using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fluent.Net
{
    public class Serializer
    {
        private static bool IncludesNewLine(Ast.SyntaxNode node)
        {
            return node is Ast.TextElement te &&
                te.Value.IndexOf('\n') >= 0;
        }

        private static bool IsSelectExpression(Ast.SyntaxNode elem)
        {
            return elem is Ast.Placeable placeable &&
                placeable.Expression is Ast.SelectExpression;
        }

        // Bit masks representing the state of the serializer.
        [Flags]
        enum State
        {
            HasEntries = 1
        };

        private readonly bool _withJunk;

        public Serializer(bool withJunk = false)
        {
            _withJunk = withJunk;
        }

        public void Serialize(TextWriter writer, Ast.Resource resource)
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            State state = 0;

            var indentingWriter = new IndentingWriter(writer);

            foreach (var entry in resource.Body)
            {
                if (this._withJunk || !(entry is Ast.Junk))
                {
                    SerializeEntry(indentingWriter, entry, state);
                    state |= State.HasEntries;
                }
            }
        }

        static bool IsFlagSet(State state, State flag)
        {
            return (state & flag) == flag;
        }

        static void SerializeEntry(IndentingWriter writer, Ast.Entry entry, State state)
        {
            if (entry is Ast.MessageTermBase messageOrTerm)
            {
                SerializeMessageOrTerm(writer, messageOrTerm);
            }
            else if (entry is Ast.BaseComment)
            {
                if (IsFlagSet(state, State.HasEntries))
                {
                    writer.Write('\n');
                }
                if (entry is Ast.Comment comment)
                {
                    SerializeComment(writer, comment);
                }
                else if (entry is Ast.GroupComment groupComment)
                {
                    SerializeGroupComment(writer, groupComment);
                }
                else if (entry is Ast.ResourceComment resourceComment)
                {
                    SerializeResourceComment(writer, resourceComment);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Unknown comment type {entry.GetType()}");
                }
                writer.Write("\n\n");
            }
            else if (entry is Ast.Junk junk)
            {
                SerializeJunk(writer, junk);
            }
            else
            {
                throw new InvalidOperationException($"Unknown entry type {entry.GetType()}");
            }
        }

        static void SerializeComment(IndentingWriter writer,
            Ast.BaseComment comment, string prefix)
        {
            var lines = comment.Content.Split('\n');
            for (int i = 0; i < lines.Length; ++i)
            {
                if (i > 0)
                {
                    writer.Write('\n');
                }

                var line = lines[i];
                if (String.IsNullOrEmpty(line))
                {
                    writer.Write(prefix);
                }
                else
                {
                    writer.Write(prefix);
                    writer.Write(' ');
                    writer.Write(line);
                }
            }
        }

        static void SerializeComment(IndentingWriter writer, Ast.Comment comment)
        {
            SerializeComment(writer, comment, "#");
        }

        static void SerializeGroupComment(IndentingWriter writer, Ast.GroupComment comment)
        {
            SerializeComment(writer, comment, "##");
        }

        static void SerializeResourceComment(IndentingWriter writer, Ast.ResourceComment comment)
        {
            SerializeComment(writer, comment, "###");
        }

        static void SerializeJunk(IndentingWriter writer, Ast.Junk junk)
        {
            if (!String.IsNullOrEmpty(junk.Content))
            {
                writer.Write(junk.Content);
            }
        }

        static void SerializeMessageOrTerm(IndentingWriter writer,
            Ast.MessageTermBase messageOrTerm)
        {
            if (messageOrTerm.Comment != null)
            {
                SerializeComment(writer, messageOrTerm.Comment);
                writer.Write("\n");
            }

            SerializeIdentifier(writer, messageOrTerm.Id);
            writer.Write(" =");

            if (messageOrTerm.Value != null)
            {
                SerializeValue(writer, messageOrTerm.Value);
            }

            if (messageOrTerm.Attributes != null)
            {
                foreach (var attribute in messageOrTerm.Attributes)
                {
                    SerializeAttribute(writer, attribute);
                }
            }
            writer.Write('\n');
        }

        static void SerializeAttribute(IndentingWriter writer, Ast.Attribute attribute)
        {
            writer.Indent();
            writer.Write("\n.");
            SerializeIdentifier(writer, attribute.Id);
            writer.Write(" =");
            SerializeValue(writer, attribute.Value);
            writer.Dedent();
        }

        static void SerializeValue(IndentingWriter writer, Ast.SyntaxNode value)
        {
            if (value is Ast.Pattern pattern)
            {
                SerializePattern(writer, pattern);
            }
            else if (value is Ast.VariantList variantList)
            {
                SerializeVariantList(writer, variantList);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unknown value type {value.GetType()}");
            }
        }

        static void SerializePattern(IndentingWriter writer, Ast.Pattern pattern)
        {
            writer.Indent();
            if (pattern.Elements.Any(IncludesNewLine) ||
                pattern.Elements.Any(IsSelectExpression))
            {
                writer.Write('\n');
            }
            else
            {
                writer.Write(' ');
            }

            foreach (var element in pattern.Elements)
            {
                SerializeElement(writer, element);
            }

            writer.Dedent();
        }

        static void SerializeVariantList(IndentingWriter writer, Ast.VariantList variantList)
        {
            writer.Indent();
            writer.Write("\n{");
            foreach (var variant in variantList.Variants)
            {
                SerializeVariant(writer, variant);
            }
            writer.Write("\n}");
            writer.Dedent();
        }

        static void SerializeElement(IndentingWriter writer,
            Ast.SyntaxNode element)
        {
            if (element is Ast.TextElement textElement)
            {
                SerializeTextElement(writer, textElement);
            }
            else if (element is Ast.Placeable placeable)
            {
                SerializePlaceable(writer, placeable);
            }
            else
            {
                throw new InvalidOperationException($"Unknown element type {element.GetType()}");
            }
        }

        static void SerializeTextElement(IndentingWriter writer,
            Ast.TextElement text)
        {
            if (!String.IsNullOrEmpty(text.Value))
            {
                writer.Write(text.Value);
            }
        }

        static void SerializePlaceable(IndentingWriter writer,
            Ast.Placeable placeable)
        {
            var expr = placeable.Expression;
            if (expr is Ast.Placeable placeableExpr)
            {
                writer.Write('{');
                SerializePlaceable(writer, placeableExpr);
                writer.Write('}');
            }
            else if (expr is Ast.SelectExpression selectExpr)
            {
                // Special-case select expression to control the whitespace around the
                // opening and the closing brace.
                writer.Write("{ ");
                // TODO: writer.Write(');
                // TODO: if (selectExpr.Selector != null)
                // TODO: {
                // TODO:     writer.Write(' ');
                // TODO: }
                SerializeSelectExpression(writer, selectExpr);
                writer.Write('}');
            }
            else
            {
                writer.Write("{ ");
                SerializeExpression(writer, expr);
                writer.Write(" }");
            }
        }

        public void SerializeExpression(TextWriter writer, Ast.SyntaxNode expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            var indentingWriter = new IndentingWriter(writer);
            SerializeExpression(indentingWriter, expression);
        }

        static void SerializeExpression(IndentingWriter writer,
            Ast.SyntaxNode expression)
        {
            if (expression is Ast.StringLiteral stringLiteral)
            {
                SerializeStringLiteral(writer, stringLiteral);
            }
            else if (expression is Ast.NumberLiteral numberLiteral)
            {
                SerializeNumberLiteral(writer, numberLiteral);
            }
            else if (expression is Ast.MessageTermReference mtReference)
            {
                SerializeMessageTermReference(writer, mtReference);
            }
            else if (expression is Ast.VariableReference varReference)
            {
                SerializeVariableReference(writer, varReference);
            }
            else if (expression is Ast.AttributeExpression attributeExpression)
            {
                SerializeAttributeExpression(writer, attributeExpression);
            }
            else if (expression is Ast.VariantExpression variantExpression)
            {
                SerializeVariantExpression(writer, variantExpression);
            }
            else if (expression is Ast.CallExpression callExpression)
            {
                SerializeCallExpression(writer, callExpression);
            }
            else if (expression is Ast.SelectExpression selectExpression)
            {
                SerializeSelectExpression(writer, selectExpression);
            }
            else if (expression is Ast.Placeable placeable)
            {
                SerializePlaceable(writer, placeable);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unknown expression type: {expression.GetType()}");
            }
        }

        static void SerializeStringLiteral(IndentingWriter writer,
            Ast.StringLiteral expr)
        {
            writer.Write('"');
            writer.Write(expr.Value);
            writer.Write('"');
        }

        static void SerializeNumberLiteral(IndentingWriter writer,
            Ast.NumberLiteral expr)
        {
            writer.Write(expr.Value);
        }

        static void SerializeMessageTermReference(IndentingWriter writer,
            Ast.MessageTermReference mtReference)
        {
            SerializeIdentifier(writer, mtReference.Id);
        }

        static void SerializeVariableReference(IndentingWriter writer,
            Ast.VariableReference varReference)
        {
            writer.Write('$');
            SerializeIdentifier(writer, varReference.Id);
        }


        static void SerializeSelectExpression(IndentingWriter writer,
            Ast.SelectExpression expr)
        {
            SerializeExpression(writer, expr.Selector);
            writer.Write(" ->");

            if (expr.Variants != null)
            {
                foreach (var variant in expr.Variants)
                {
                    SerializeVariant(writer, variant);
                }
            }
            writer.Write('\n');
        }

        static void SerializeVariant(IndentingWriter writer,
            Ast.Variant variant)
        {
            writer.Write('\n');
            // squiffy indentation: 3 spaces for default, otherwise 4
            if (variant.IsDefault)
            {
                writer.Write("   *");
            }
            else
            {
                writer.Write("    ");
            }
            writer.Write('[');
            SerializeVariantKey(writer, variant.Key);
            writer.Write(']');
            writer.Indent();
            SerializeValue(writer, variant.Value);
            writer.Dedent();
        }

        static void SerializeAttributeExpression(IndentingWriter writer,
            Ast.AttributeExpression expr)
        {
            SerializeExpression(writer, expr.Ref);
            writer.Write('.');
            SerializeIdentifier(writer, expr.Name);
        }

        static void SerializeVariantExpression(IndentingWriter writer,
            Ast.VariantExpression expr)
        {
            SerializeExpression(writer, expr.Reference);
            writer.Write('[');
            SerializeVariantKey(writer, expr.Key);
            writer.Write(']');
        }

        static void SerializeArgumentList<T>(IndentingWriter writer,
            ref bool first, IReadOnlyList<T> args, Action<T> serializeArg)
        {
            foreach (var arg in args)
            {
                if (!first)
                {
                    writer.Write(", ");
                }
                first = false;
                serializeArg(arg);
            }
        }


        static void SerializeCallExpression(IndentingWriter writer,
            Ast.CallExpression expr)
        {
            SerializeFunction(writer, expr.Callee);
            writer.Write('(');
            bool first = true;
            if (expr.Positional != null)
            {
                SerializeArgumentList(writer, ref first, expr.Positional,
                    (arg) => SerializeExpression(writer, arg));
            }
            if (expr.Named != null)
            {
                SerializeArgumentList(writer, ref first, expr.Named,
                    (arg) => SerializeNamedArgument(writer, arg));
            }
            writer.Write(')');
        }

        static void SerializeNamedArgument(IndentingWriter writer,
            Ast.NamedArgument arg)
        {
            SerializeIdentifier(writer, arg.Name);
            writer.Write(": ");
            SerializeArgumentValue(writer, arg.Value);
        }

        static void SerializeArgumentValue(IndentingWriter writer,
            Ast.Expression argValue)
        {
            if (argValue is Ast.StringLiteral stringLiteral)
            {
                SerializeStringLiteral(writer, stringLiteral);
            }
            else if (argValue is Ast.NumberLiteral numberLiteral)
            {
                SerializeNumberLiteral(writer, numberLiteral);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unknown argument type: {argValue.GetType()}");
            }
        }

        static void SerializeIdentifier(IndentingWriter writer,
            Ast.Identifier id)
        {
            if (!String.IsNullOrEmpty(id.Name))
            {
                writer.Write(id.Name);
            }
        }

        static void SerializeVariantName(IndentingWriter writer,
            Ast.VariantName name)
        {
            if (!String.IsNullOrEmpty(name.Name))
            {
                writer.Write(name.Name);
            }
        }

        static void SerializeVariantKey(IndentingWriter writer,
            Ast.SyntaxNode key)
        {
            if (key is Ast.VariantName variantName)
            {
                SerializeVariantName(writer, variantName);
            }
            else if (key is Ast.NumberLiteral numberLiteral)
            {
                SerializeNumberLiteral(writer, numberLiteral);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unknown variant key type: {key.GetType()}");
            }
        }

        static void SerializeFunction(IndentingWriter writer,
            Ast.Function fun /* it wasn't */)
        {
            writer.Write(fun.Name);
        }
    }
}
