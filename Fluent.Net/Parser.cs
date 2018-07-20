using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Fluent.Net
{
    public class Parser
    {
        const int Eof = ParserStream.Eof;
        internal static Regex s_fnName = new Regex("^[A-Z][A-Z_?-]*$", RegexOptions.Compiled);

        bool _withSpans;
        bool _lastCommentZeroFourSyntax = false;

        public Parser(bool withSpans = true)
        {
            _withSpans = withSpans;
        }

        T SpanWrapper<T>(FtlParserStream ps, Func<T> wrappedFn) where T : Ast.SyntaxNode
        {
            int start = ps.GetIndex();
            var node = wrappedFn();

            // Don't re-add the span if the node already has it.  This may happen when
            // one decorated function calls another decorated function.
            if (node.Span != null)
            {
                return node;
            }

            // Spans of Messages and Sections should include the attached Comment.
            if (node is Ast.Message message)
            {
                if (message.Comment != null)
                {
                    start = message.Comment.Span.Start;
                }
            }

            var end = ps.GetIndex();
            node.AddSpan(start, end);
            return node;
        }

        T SpanWrapper<T>(FtlParserStream ps, Func<FtlParserStream, T> wrappedFn) where T : Ast.SyntaxNode
        {
            return SpanWrapper(ps, () => wrappedFn(ps));
        }

        public Ast.Resource Parse(TextReader input)
        {
            var ps = new FtlParserStream(input);
            ps.SkipBlankLines();

            var entries = new List<Ast.Entry>();

            while (ps.Current != Eof)
            {
                var entry = GetEntryOrJunk(ps);

                if (entry == null)
                {
                    // That happens when we get a 0.4 style section
                    continue;
                }

                if (entry is Ast.Comment comment &&
                    _lastCommentZeroFourSyntax && entries.Count == 0)
                {
                    var resourceComment = new Ast.ResourceComment(comment.Content);
                    resourceComment.Span = comment.Span;
                    entries.Add(resourceComment);
                }
                else
                {
                    entries.Add(entry);
                }

                _lastCommentZeroFourSyntax = false;
                ps.SkipBlankLines();
            }

            var res = new Ast.Resource(entries);

            if (_withSpans)
            {
                res.AddSpan(0, ps.GetIndex());
            }

            return res;
        }

        public Ast.Entry ParseEntry(TextReader source)
        {
            var ps = new FtlParserStream(source);
            ps.SkipBlankLines();
            return GetEntryOrJunk(ps);
        }

        Ast.Entry GetEntryOrJunk(FtlParserStream ps)
        {
            int entryStartPos = ps.GetIndex();
            ps.BeginCapture();

            try
            {
                return GetEntry(ps);
            }
            catch (ParseException e)
            {
                int errorIndex = ps.GetIndex();
                ps.SkipToNextEntryStart();
                int nextEntryStart = ps.GetIndex();

                // Create a Junk instance
                var junk = new Ast.Junk(ps.GetCapturedText());
                if (_withSpans)
                {
                    junk.AddSpan(entryStartPos, nextEntryStart);
                }
                var annot = new Ast.Annotation(e.Code, e.Args, e.Message);
                annot.AddSpan(errorIndex, errorIndex);
                junk.AddAnnotation(annot);
                return junk;
            }
        }

        public Ast.Entry GetEntry(FtlParserStream ps)
        {
            Ast.BaseComment comment = null;

            if (ps.CurrentIs('/') || ps.CurrentIs('#'))
            {
                comment = GetComment(ps);

                // The Comment content doesn't include the trailing newline. Consume
                // this newline here to be ready for the next entry.
                if (ps.Current != Eof)
                {
                    ps.ExpectNewLine();
                }
            }

            if (ps.CurrentIs('['))
            {
                var groupComment = GetGroupCommentFromSection(ps, comment);
                if (comment != null && _withSpans)
                {
                    // The Group Comment should start where the section comment starts.
                    groupComment.Span.Start = comment.Span.Start;
                }
                return groupComment;
            }

            if (ps.IsEntryIDStart() && (comment == null || comment is Ast.Comment))
            {
                return GetMessage(ps, comment as Ast.Comment);
            }

            if (comment != null)
            {
                return comment;
            }

            throw new ParseException("E0002");
        }

        Ast.Comment GetZeroFourStyleComment(FtlParserStream ps)
        {
            ps.ExpectChar('/');
            ps.ExpectChar('/');
            ps.TakeCharIf(' ');

            var content = new StringBuilder();

            while (true)
            {
                int ch;
                while ((ch = ps.TakeChar(x => x != '\r' && x != '\n')) != Eof)
                {
                    content.Append((char)ch);
                }

                if (ps.IsPeekNextLineZeroFourStyleComment())
                {
                    content.Append('\n');
                    ps.SkipNewLine();
                    ps.ExpectChar('/');
                    ps.ExpectChar('/');
                    ps.TakeCharIf(' ');
                }
                else
                {
                    break;
                }
            }

            var comment = new Ast.Comment(content.ToString());
            _lastCommentZeroFourSyntax = true;
            return comment;
        }

        public Ast.BaseComment _GetComment(FtlParserStream ps)
        {
            if (ps.CurrentIs('/'))
            {
                return GetZeroFourStyleComment(ps);
            }

            // 0 - comment
            // 1 - group comment
            // 2 - resource comment
            int level = -1;
            var content = new StringBuilder();

            while (true)
            {
                int i = -1;
                while (ps.CurrentIs('#') && (i < (level == -1 ? 2 : level)))
                {
                    ps.Next();
                    i++;
                }

                if (level == -1)
                {
                    level = i;
                }

                if (!ps.IsPeekNewLine())
                {
                    ps.ExpectChar(' ');
                    int ch;
                    while ((ch = ps.TakeChar(x => x != '\r' && x != '\n')) != Eof)
                    {
                        content.Append((char)ch);
                    }
                }

                if (ps.IsPeekNextLineComment(level))
                {
                    content.Append('\n');
                    ps.SkipNewLine();
                }
                else
                {
                    break;
                }
            }

            var text = content.ToString();
            switch (level)
            {
                case 0:
                    return new Ast.Comment(text);
                case 1:
                    return new Ast.GroupComment(text);
                case 2:
                    return new Ast.ResourceComment(text);
            }
            throw new InvalidOperationException($"Unknown level value '{level}'");
        }

        public Ast.BaseComment GetComment(FtlParserStream ps) =>
            SpanWrapper(ps, _GetComment);

        public Ast.GroupComment _GetGroupCommentFromSection(FtlParserStream ps, Ast.BaseComment comment)
        {
            ps.ExpectChar('[');
            ps.ExpectChar('[');

            ps.SkipInlineWs();

            GetVariantName(ps);

            ps.SkipInlineWs();

            ps.ExpectChar(']');
            ps.ExpectChar(']');

            if (comment != null)
            {
                return new Ast.GroupComment(comment.Content);
            }

            // A Section without a comment is like an empty Group Comment. Semantically
            // it ends the previous group and starts a new one.
            return new Ast.GroupComment("");
        }

        public Ast.GroupComment GetGroupCommentFromSection(FtlParserStream ps, Ast.BaseComment comment) =>
            SpanWrapper(ps, () => _GetGroupCommentFromSection(ps, comment));

        Ast.Entry _GetMessage(FtlParserStream ps, Ast.Comment comment)
        {
            var id = GetEntryIdentifier(ps);

            ps.SkipInlineWs();

            Ast.Pattern pattern = null;
            IReadOnlyList<Ast.Attribute> attrs= null;

            // XXX Syntax 0.4 compatibility.
            // XXX Replace with ps.ExpectChar('=').
            if (ps.CurrentIs('='))
            {
                ps.Next();

                if (ps.IsPeekPatternStart())
                {
                    ps.SkipIndent();
                    pattern = GetPattern(ps);
                }
                else
                {
                    ps.SkipInlineWs();
                }
            }

            if (id.Name.StartsWith("-") && pattern == null)
            {
                throw new ParseException("E0006", id.Name);
            }

            if (ps.IsPeekNextLineAttributeStart())
            {
                attrs = GetAttributes(ps);
            }

            if (id.Name.StartsWith("-"))
            {
                return new Ast.Term(id, pattern, attrs, comment);
            }

            if (pattern == null && attrs == null)
            {
                throw new ParseException("E0005", id.Name);
            }

            return new Ast.Message(id, pattern, attrs, comment);
        }

        Ast.Entry GetMessage(FtlParserStream ps, Ast.Comment comment) =>
            SpanWrapper(ps, () => _GetMessage(ps, comment));

        Ast.Attribute _GetAttribute(FtlParserStream ps)
        {
            ps.ExpectChar('.');

            var key = GetIdentifier(ps);

            ps.SkipInlineWs();
            ps.ExpectChar('=');

            if (ps.IsPeekPatternStart())
            {
                ps.SkipIndent();
                var value = GetPattern(ps);
                return new Ast.Attribute(key, value);
            }

            throw new ParseException("E0012");
        }
        
        Ast.Attribute GetAttribute(FtlParserStream ps) =>
            SpanWrapper(ps, _GetAttribute);

        IReadOnlyList<Ast.Attribute> GetAttributes(FtlParserStream ps)
        {
            var attrs = new List<Ast.Attribute>();

            while (true)
            {
                ps.ExpectIndent();
                var attr = GetAttribute(ps);
                attrs.Add(attr);

                if (!ps.IsPeekNextLineAttributeStart())
                {
                    break;
                }
            }
            return attrs;
        }

        Ast.Identifier GetEntryIdentifier(FtlParserStream ps)
        {
            return GetIdentifier(ps, true);
        }

        Ast.Identifier _GetIdentifier(FtlParserStream ps, bool allowTerm = false)
        {
            var name = new StringBuilder();
            name.Append((char)ps.TakeIDStart(allowTerm));

            int ch;
            while ((ch = ps.TakeIDChar()) != Eof)
            {
                name.Append((char)ch);
            }

            return new Ast.Identifier(name.ToString());
        }

        Ast.Identifier GetIdentifier(FtlParserStream ps, bool allowTerm = false) =>
            SpanWrapper(ps, () => _GetIdentifier(ps, allowTerm));

        Ast.SyntaxNode GetVariantKey(FtlParserStream ps)
        {
            var ch = ps.Current;

            if (ch == Eof)
            {
                throw new ParseException("E0013");
            }

            if ((ch >= '0' && ch <= '9') || ch == '-')
            {
                return GetNumber(ps);
            }

            return GetVariantName(ps);
        }

        Ast.Variant _GetVariant(FtlParserStream ps, bool hasDefault)
        {
            bool defaultIndex = false;

            if (ps.CurrentIs('*'))
            {
                if (hasDefault)
                {
                    throw new ParseException("E0015");
                }
                ps.Next();
                defaultIndex = true;
                hasDefault = true;
            }

            ps.ExpectChar('[');

            var key = GetVariantKey(ps);

            ps.ExpectChar(']');

            if (ps.IsPeekPatternStart())
            {
                ps.SkipIndent();
                var value = GetPattern(ps);
                return new Ast.Variant(key, value, defaultIndex);
            }

            throw new ParseException("E0012");
        }

        Ast.Variant GetVariant(FtlParserStream ps, bool hasDefault) =>
            SpanWrapper(ps, () => _GetVariant(ps, hasDefault));

        IReadOnlyList<Ast.Variant> GetVariants(FtlParserStream ps)
        {
            var variants = new List<Ast.Variant>();
            bool hasDefault = false;

            while (true)
            {
                ps.ExpectIndent();
                var variant = GetVariant(ps, hasDefault);

                if (variant.IsDefault)
                {
                    hasDefault = true;
                }

                variants.Add(variant);

                if (!ps.IsPeekNextLineVariantStart())
                {
                    break;
                }
            }

            if (!hasDefault)
            {
                throw new ParseException("E0010");
            }

            return variants;
        }

        Ast.VariantName _GetVariantName(FtlParserStream ps)
        {
            var name = new StringBuilder();

            name.Append((char)ps.TakeIDStart(false));

            while (true)
            {
                var ch = ps.TakeVariantNameChar();
                if (ch != Eof)
                {
                    name.Append((char)ch);
                }
                else
                {
                    break;
                }
            }

            return new Ast.VariantName(name.ToString().TrimEnd());
        }

        Ast.VariantName GetVariantName(FtlParserStream ps) =>
            SpanWrapper(ps, _GetVariantName);

        string GetDigits(FtlParserStream ps)
        {
            var num = new StringBuilder();

            int ch;
            while ((ch = ps.TakeDigit()) != Eof)
            {
                num.Append((char)ch);
            }

            if (num.Length == 0)
            {
                throw new ParseException("E0004", "0-9");
            }

            return num.ToString();
        }

        Ast.NumberExpression _GetNumber(FtlParserStream ps)
        {
            var num = new StringBuilder();

            if (ps.CurrentIs('-'))
            {
                num.Append('-');
                ps.Next();
            }

            num.Append(GetDigits(ps));

            if (ps.CurrentIs('.'))
            {
                num.Append('.');
                ps.Next();
                num.Append(GetDigits(ps));
            }

            return new Ast.NumberExpression(num.ToString());
        }

        Ast.NumberExpression GetNumber(FtlParserStream ps) =>
            SpanWrapper(ps, _GetNumber);

        Ast.Pattern _GetPattern(FtlParserStream ps)
        {
            var elements = new List<Ast.SyntaxNode>();
            ps.SkipInlineWs();

            int ch;
            while ((ch = ps.Current) != Eof)
            {
                // The end condition for GetPattern's while loop is a newline
                // which is not followed by a valid pattern continuation.
                if (ps.IsPeekNewLine() && !ps.IsPeekNextLinePatternStart())
                {
                    break;
                }

                if (ch == '{')
                {
                    var element = GetPlaceable(ps);
                    elements.Add(element);
                }
                else
                {
                    var element = GetTextElement(ps);
                    elements.Add(element);
                }
            }

            return new Ast.Pattern(elements);
        }

        Ast.Pattern GetPattern(FtlParserStream ps) =>
            SpanWrapper(ps, _GetPattern);

        Ast.TextElement _GetTextElement(FtlParserStream ps)
        {
            var buffer = new StringBuilder();

            int ch;
            while ((ch = ps.Current) != Eof)
            {
                if (ch == '{')
                {
                    return new Ast.TextElement(buffer.ToString());
                }

                if (ch == '\r' || ch == '\n')
                {
                    if (!ps.IsPeekNextLinePatternStart())
                    {
                        return new Ast.TextElement(buffer.ToString());
                    }

                    ps.SkipNewLine();
                    ps.SkipInlineWs();

                    // Add the new line to the buffer
                    buffer.Append((char)ch);
                    continue;
                }

                if (ch == '\\')
                {
                    var ch2 = ps.Next();

                    if (ch2 == '{' || ch2 == '"')
                    {
                        buffer.Append((char)ch2);
                    }
                    else
                    {
                        buffer.Append((char)ch).Append((char)ch2);
                    }

                }
                else
                {
                    buffer.Append((char)ps.Current);
                }

                ps.Next();
            }

            return new Ast.TextElement(buffer.ToString());
        }

        Ast.TextElement GetTextElement(FtlParserStream ps) =>
            SpanWrapper(ps, _GetTextElement);

        Ast.Placeable _GetPlaceable(FtlParserStream ps)
        {
            ps.ExpectChar('{');
            var expression = GetExpression(ps);
            ps.ExpectChar('}');
            return new Ast.Placeable(expression);
        }

        Ast.Placeable GetPlaceable(FtlParserStream ps) =>
            SpanWrapper(ps, _GetPlaceable);

        Ast.Expression _GetExpression(FtlParserStream ps)
        {
            if (ps.IsPeekNextLineVariantStart())
            {
                var variants = GetVariants(ps);

                ps.ExpectIndent();

                return new Ast.SelectExpression(null, variants);
            }

            ps.SkipInlineWs();

            var selector = GetSelectorExpression(ps);

            ps.SkipInlineWs();

            if (ps.CurrentIs('-'))
            {
                ps.Peek();

                if (!ps.CurrentPeekIs('>'))
                {
                    ps.ResetPeek();
                    return selector;
                }

                if (selector is Ast.MessageReference)
                {
                    throw new ParseException("E0016");
                }

                if (selector is Ast.AttributeExpression ae &&
                    !ae.Id.Name.StartsWith("-"))
                {
                    throw new ParseException("E0018");
                }

                if (selector is Ast.VariantExpression)
                {
                    throw new ParseException("E0017");
                }

                ps.Next();
                ps.Next();

                ps.SkipInlineWs();

                var variants = GetVariants(ps);

                if (variants.Count == 0)
                {
                    throw new ParseException("E0011");
                }

                ps.ExpectIndent();

                return new Ast.SelectExpression(selector, variants);
            }
            else if (selector is Ast.AttributeExpression ae &&
                     ae.Id.Name.StartsWith("-"))
            {
                throw new ParseException("E0019");
            }

            return selector;
        }

        Ast.Expression GetExpression(FtlParserStream ps) =>
            SpanWrapper(ps, _GetExpression);

        Ast.Expression _GetSelectorExpression(FtlParserStream ps)
        {
            var literal = GetLiteral(ps);

            var messageReference = literal as Ast.MessageReference;
            if (messageReference == null)
            {
                return literal;
            }

            var ch = ps.Current;

            if (ch == '.')
            {
                ps.Next();

                var attr = GetIdentifier(ps);
                return new Ast.AttributeExpression(messageReference.Id, attr);
            }

            if (ch == '[')
            {
                ps.Next();

                var key = GetVariantKey(ps);

                ps.ExpectChar(']');

                return new Ast.VariantExpression(literal, key);
            }

            if (ch == '(')
            {
                ps.Next();

                var args = GetCallArgs(ps);

                ps.ExpectChar(')');

                if (!s_fnName.IsMatch(messageReference.Id.Name))
                {
                    throw new ParseException("E0008");
                }

                var func = new Ast.Function(messageReference.Id.Name);
                if (_withSpans)
                {
                    func.AddSpan(messageReference.Span.Start, messageReference.Span.End);
                }

                return new Ast.CallExpression(func, args);
            }

            return literal;
        }

        Ast.Expression GetSelectorExpression(FtlParserStream ps) =>
            SpanWrapper(ps, _GetSelectorExpression);

        Ast.SyntaxNode _GetCallArg(FtlParserStream ps)
        {
            var exp = GetSelectorExpression(ps);

            ps.SkipInlineWs();

            if (ps.Current != ':')
            {
                return exp;
            }

            var messageReference = exp as Ast.MessageReference;
            if (messageReference == null)
            {
                throw new ParseException("E0009");
            }

            ps.Next();
            ps.SkipInlineWs();

            var val = GetArgVal(ps);
            return new Ast.NamedArgument(messageReference.Id, val);
        }

        Ast.SyntaxNode GetCallArg(FtlParserStream ps) =>
            SpanWrapper(ps, _GetCallArg);

        IReadOnlyList<Ast.SyntaxNode> GetCallArgs(FtlParserStream ps)
        {
            var args = new List<Ast.SyntaxNode>();

            ps.SkipInlineWs();

            while (true)
            {
                if (ps.Current == ')')
                {
                    break;
                }

                var arg = GetCallArg(ps);
                args.Add(arg);

                ps.SkipInlineWs();

                if (ps.Current == ',')
                {
                    ps.Next();
                    ps.SkipInlineWs();
                    continue;
                }
                else
                {
                    break;
                }
            }
            return args;
        }

        Ast.Expression GetArgVal(FtlParserStream ps)
        {
            if (ps.IsNumberStart())
            {
                return GetNumber(ps);
            }
            else if (ps.CurrentIs('"'))
            {
                return GetString(ps);
            }
            throw new ParseException("E0012");
        }

        Ast.StringExpression _GetString(FtlParserStream ps)
        {
            var val = new StringBuilder();

            ps.ExpectChar('"');

            int ch;
            while ((ch = ps.TakeChar(x => x != '"' && x != '\r' && x != '\n')) != Eof)
            {
                val.Append((char)ch);
            }

            if (ps.CurrentIs('\r') || ps.CurrentIs('\n'))
            {
                throw new ParseException("E0020");
            }

            ps.Next();

            return new Ast.StringExpression(val.ToString());
        }

        Ast.StringExpression GetString(FtlParserStream ps) =>
            SpanWrapper(ps, _GetString);

        Ast.Expression _GetLiteral(FtlParserStream ps)
        {
            var ch = ps.Current;

            if (ch == Eof)
            {
                throw new ParseException("E0014");
            }

            if (ch == '$')
            {
                ps.Next();
                var name = GetIdentifier(ps);
                return new Ast.ExternalArgument(name);
            }

            if (ps.IsEntryIDStart())
            {
                var name = GetEntryIdentifier(ps);
                return new Ast.MessageReference(name);
            }

            if (ps.IsNumberStart())
            {
                return GetNumber(ps);
            }

            if (ch == '"')
            {
                return GetString(ps);
            }

            throw new ParseException("E0014");
        }

        Ast.Expression GetLiteral(FtlParserStream ps) =>
            SpanWrapper(ps, _GetLiteral);
    }
}
