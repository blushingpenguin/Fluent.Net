using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Fluent.Net
{
    public class Parser
    {
        const int Eof = ParserStream.Eof;
        internal static Regex s_fnName = new Regex("^[A-Z][A-Z_?-]*$", RegexOptions.Compiled);

        bool _withSpans;

        public Parser(bool withSpans = true)
        {
            _withSpans = withSpans;
        }

        T SpanWrapper<T>(FtlParserStream ps, Func<T> wrappedFn) where T : Ast.SyntaxNode
        {
            if (!_withSpans)
            {
                return wrappedFn();
            }

            var start = ps.GetPosition();
            var node = wrappedFn();

            // Don't re-add the span if the node already has it.  This may happen when
            // one decorated function calls another decorated function.
            if (node.Span != null)
            {
                return node;
            }

            var end = ps.GetPosition();
            node.AddSpan(start, end);
            return node;
        }

        T SpanWrapper<T>(FtlParserStream ps, Func<FtlParserStream, T> wrappedFn) where T : Ast.SyntaxNode
        {
            return SpanWrapper(ps, () => wrappedFn(ps));
        }

        public static string TrimRight(string s)
        {
            int end = s.Length;
            for (; end > 0 && FtlParserStream.IsWhite(s[end - 1]); --end)
            {
            }
            return s.Length == end ? s : s.Substring(0, end);
        }

        public Ast.Resource Parse(TextReader input)
        {
            var ps = new FtlParserStream(input);
            ps.SkipBlankLines();

            var entries = new List<Ast.Entry>();
            Ast.Comment lastComment = null;

            while (ps.Current != Eof)
            {
                var entry = GetEntryOrJunk(ps);

                int blankLines = ps.SkipBlankLines();
                // Regular Comments require special logic. Comments may be attached to
                // Messages or Terms if they are followed immediately by them. However
                // they should parse as standalone when they're followed by Junk.
                // Consequently, we only attach Comments once we know that the Message
                // or the Term parsed successfully.
                if (entry is Ast.Comment comment && 
                    blankLines == 0 && ps.Current != Eof)
                {
                    // Stash the comment and decide what to do with it in the next pass.
                    lastComment = comment;
                    continue;
                }

                if (lastComment != null)
                {
                    if (entry is Ast.MessageTermBase mt)
                    {
                        mt.Comment = lastComment;
                        if (_withSpans)
                        {
                            mt.Span.Start = lastComment.Span.Start;
                        }
                    }
                    else
                    {
                        entries.Add(lastComment);
                    }
                    // In either case, the stashed comment has been dealt with; clear it.
                    lastComment = null;
                }
                
                // No special logic for other types of entries.
                entries.Add(entry);
            }

            var res = new Ast.Resource(entries);

            if (_withSpans)
            {
                res.AddSpan(Position.Start, ps.GetPosition());
            }

            return res;
        }

        /// <summary>
        /// Parse the first Message or Term in `source`.
        /// 
        /// Skip all encountered comments and start parsing at the first Message or
        /// Term start. Return Junk if the parsing is not successful.
        /// 
        /// Preceding comments are ignored unless they contain syntax errors
        /// themselves, in which case Junk for the invalid comment is returned.
        /// </summary>
        public Ast.Entry ParseEntry(TextReader source)
        {
            var ps = new FtlParserStream(source);
            ps.SkipBlankLines();

            while (ps.CurrentIs('#'))
            {
                var skipped = GetEntryOrJunk(ps);
                if (skipped is Ast.Junk)
                {
                    // Don't skip Junk comments.
                    return skipped;
                }
                ps.SkipBlankLines();
            }

            return GetEntryOrJunk(ps);
        }

        Ast.Entry GetEntryOrJunk(FtlParserStream ps)
        {
            var entryStartPos = ps.GetPosition();
            ps.BeginCapture();

            try
            {
                var entry = GetEntry(ps);
                ps.ExpectNewLine();
                return entry;
            }
            catch (ParseException e)
            {
                var errorPos = ps.GetPosition();
                ps.SkipToNextEntryStart();
                var nextEntryStart = ps.GetPosition();

                // Create a Junk instance
                var junk = new Ast.Junk(ps.GetCapturedText());
                if (_withSpans)
                {
                    junk.AddSpan(entryStartPos, nextEntryStart);
                }
                var annot = new Ast.Annotation(e.Code, e.Args, e.Message);
                annot.AddSpan(errorPos, errorPos);
                junk.AddAnnotation(annot);
                return junk;
            }
        }

        public Ast.Entry GetEntry(FtlParserStream ps)
        {
            if (ps.CurrentIs('#'))
            {
                return GetComment(ps);
            }

            if (ps.CurrentIs('-'))
            {
                return GetTerm(ps);
            }

            if (ps.IsIdentifierStart())
            {
                return GetMessage(ps);
            }

            throw new ParseException("E0002");
        }

        public Ast.BaseComment _GetComment(FtlParserStream ps)
        {
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

        Ast.Entry _GetMessage(FtlParserStream ps)
        {
            var id = GetIdentifier(ps);

            ps.SkipInlineWs();
            ps.ExpectChar('=');

            Ast.Pattern pattern = null;
            if (ps.IsPeekValueStart())
            {
                ps.SkipIndent();
                pattern = GetPattern(ps);
            }
            else
            {
                ps.SkipInlineWs();
            }

            IReadOnlyList<Ast.Attribute> attrs = null;
            if (ps.IsPeekNextLineAttributeStart())
            {
                attrs = GetAttributes(ps);
            }

            if (pattern == null && attrs == null)
            {
                throw new ParseException("E0005", id.Name);
            }

            return new Ast.Message(id, pattern, attrs);
        }

        Ast.Entry GetMessage(FtlParserStream ps) =>
            SpanWrapper(ps, () => _GetMessage(ps));

        Ast.Entry _GetTerm(FtlParserStream ps)
        {
            var id = GetTermIdentifier(ps);

            ps.SkipInlineWs();
            ps.ExpectChar('=');

            Ast.SyntaxNode value = null;
            if (ps.IsPeekValueStart())
            {
                ps.SkipIndent();
                value = GetValue(ps);
            }
            else
            {
                throw new ParseException("E0006", id.Name);
            }

            IReadOnlyList<Ast.Attribute> attrs = null;
            if (ps.IsPeekNextLineAttributeStart())
            {
                attrs = GetAttributes(ps);
            }
            return new Ast.Term(id, value, attrs);
        }

        Ast.Entry GetTerm(FtlParserStream ps) =>
            SpanWrapper(ps, _GetTerm);

        Ast.Attribute _GetAttribute(FtlParserStream ps)
        {
            ps.ExpectChar('.');

            var key = GetIdentifier(ps);

            ps.SkipInlineWs();
            ps.ExpectChar('=');

            if (ps.IsPeekValueStart())
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

        Ast.Identifier _GetIdentifier(FtlParserStream ps)
        {
            var name = new StringBuilder();
            name.Append((char)ps.TakeIDStart());

            int ch;
            while ((ch = ps.TakeIDChar()) != Eof)
            {
                name.Append((char)ch);
            }

            return new Ast.Identifier(name.ToString());
        }

        Ast.Identifier GetIdentifier(FtlParserStream ps) =>
            SpanWrapper(ps, _GetIdentifier);

        Ast.Identifier _GetTermIdentifier(FtlParserStream ps)
        {
            ps.ExpectChar('-');
            var id = this.GetIdentifier(ps);
            return new Ast.Identifier($"-{id.Name}");
        }

        Ast.Identifier GetTermIdentifier(FtlParserStream ps) =>
            SpanWrapper(ps, _GetTermIdentifier);

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

            if (ps.IsPeekValueStart())
            {
                ps.SkipIndent();
                var value = GetValue(ps);
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
            name.Append((char)ps.TakeIDStart());

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

            return new Ast.VariantName(TrimRight(name.ToString()));
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

        Ast.NumberLiteral _GetNumber(FtlParserStream ps)
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

            return new Ast.NumberLiteral(num.ToString());
        }

        Ast.NumberLiteral GetNumber(FtlParserStream ps) =>
            SpanWrapper(ps, _GetNumber);

        Ast.SyntaxNode _GetValue(FtlParserStream ps)
        {
            if (ps.CurrentIs('{'))
            {
                ps.Peek();
                ps.PeekInlineWs();
                if (ps.IsPeekNextLineVariantStart())
                {
                    return GetVariantList(ps);
                }
            }
            return GetPattern(ps);
        }

        Ast.SyntaxNode GetValue(FtlParserStream ps) =>
            SpanWrapper(ps, _GetValue);

        Ast.SyntaxNode _GetVariantList(FtlParserStream ps)
        {
            ps.ExpectChar('{');
            ps.SkipInlineWs();
            var variants = GetVariants(ps);
            ps.ExpectIndent();
            ps.ExpectChar('}');
            return new Ast.VariantList(variants);
        }

        Ast.SyntaxNode GetVariantList(FtlParserStream ps) =>
            SpanWrapper(ps, _GetVariantList);

        Ast.Pattern _GetPattern(FtlParserStream ps)
        {
            var elements = new List<Ast.SyntaxNode>();
            ps.SkipInlineWs();

            int ch;
            while ((ch = ps.Current) != Eof)
            {
                // The end condition for GetPattern's while loop is a newline
                // which is not followed by a valid pattern continuation.
                if (ps.IsPeekNewLine() && !ps.IsPeekNextLineValue())
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

            // Trim trailing whitespace.
            if (elements.Count > 0 &&
                elements[elements.Count - 1] is Ast.TextElement te)
            {
                te.Value = TrimRight(te.Value);
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
                    if (!ps.IsPeekNextLineValue())
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
                    ps.Next();
                    GetEscapeSequence(ps,
                        new int[] { '{', '\\' }, buffer);
                    continue;
                }

                buffer.Append((char)ps.Current);
                ps.Next();
            }

            return new Ast.TextElement(buffer.ToString());
        }

        Ast.TextElement GetTextElement(FtlParserStream ps) =>
            SpanWrapper(ps, _GetTextElement);

        void GetEscapeSequence(FtlParserStream ps, int[] specials,
            StringBuilder buffer)
        {
            int next = ps.Current;
            if (Array.IndexOf(specials, next) >= 0)
            {
                ps.Next();
                buffer.Append('\\').Append((char)next);
                return;
            }

            if (next == 'u')
            {
                ps.Next();

                char[] sequence = new char[4];
                for (int i = 0; i < 4; ++i)
                {
                    int ch = ps.TakeHexDigit();
                    if (ch == Eof)
                    {
                        var msg = new String(sequence, 0, i);
                        if (ps.Current != Eof)
                        {
                            msg += (char)ps.Current;
                        }
                        throw new ParseException("E0026", msg);
                    }
                    sequence[i] = (char)ch;
                }
                buffer.Append("\\u").Append(sequence);
                return;
            }

            throw new ParseException("E0025",
                next == Eof ? "" : ((char)next).ToString());
        }

        Ast.Placeable _GetPlaceable(FtlParserStream ps)
        {
            ps.ExpectChar('{');
            var expression = GetExpression(ps);
            ps.ExpectChar('}');
            return new Ast.Placeable(expression);
        }

        Ast.Placeable GetPlaceable(FtlParserStream ps) =>
            SpanWrapper(ps, _GetPlaceable);

        Ast.SyntaxNode _GetExpression(FtlParserStream ps)
        {
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
                    ae.Ref is Ast.MessageReference)
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

                // VariantLists are only allowed in other VariantLists.
                if (variants.Any(v => v.Value is Ast.VariantList))
                {
                    throw new ParseException("E0023");
                }

                ps.ExpectIndent();

                return new Ast.SelectExpression(selector, variants);
            }
            else if (selector is Ast.AttributeExpression ae &&
                     ae.Ref is Ast.TermReference)
            {
                throw new ParseException("E0019");
            }

            return selector;
        }

        Ast.SyntaxNode GetExpression(FtlParserStream ps) =>
            SpanWrapper(ps, _GetExpression);

        Ast.SyntaxNode _GetSelectorExpression(FtlParserStream ps)
        {
            if (ps.CurrentIs('{'))
            {
                return GetPlaceable(ps);
            }

            var literal = GetLiteral(ps);

            var mtReference = literal as Ast.MessageTermReference;
            if (mtReference == null)
            {
                return literal;
            }

            var ch = ps.Current;

            if (ch == '.')
            {
                ps.Next();

                var attr = GetIdentifier(ps);
                return new Ast.AttributeExpression(mtReference, attr);
            }

            if (ch == '[')
            {
                ps.Next();

                if (mtReference is Ast.MessageReference)
                {
                    throw new ParseException("E0024");
                }

                var key = GetVariantKey(ps);

                ps.ExpectChar(']');

                return new Ast.VariantExpression(literal, key);
            }

            if (ch == '(')
            {
                ps.Next();

                if (!s_fnName.IsMatch(mtReference.Id.Name))
                {
                    throw new ParseException("E0008");
                }

                var args = GetCallArgs(ps);

                ps.ExpectChar(')');

                var func = new Ast.Function(mtReference.Id.Name);
                if (_withSpans)
                {
                    func.AddSpan(mtReference.Span.Start, mtReference.Span.End);
                }

                return new Ast.CallExpression(func, args.Positional, args.Named);
            }

            return literal;
        }

        Ast.SyntaxNode GetSelectorExpression(FtlParserStream ps) =>
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

        class CallArgs
        {
            public List<Ast.SyntaxNode> Positional { get; } =
                new List<Ast.SyntaxNode>();
            public List<Ast.NamedArgument> Named { get; } =
                new List<Ast.NamedArgument>();
        }

        CallArgs GetCallArgs(FtlParserStream ps)
        {
            var result = new CallArgs();
            var argumentNames = new HashSet<string>();

            ps.SkipInlineWs();
            ps.SkipIndent();

            while (true)
            {
                if (ps.Current == ')')
                {
                    break;
                }

                var arg = GetCallArg(ps);
                if (arg is Ast.NamedArgument narg)
                {
                    if (argumentNames.Contains(narg.Name.Name))
                    {
                        throw new ParseException("E0022");
                    }
                    result.Named.Add(narg);
                    argumentNames.Add(narg.Name.Name);
                }
                else if (argumentNames.Count > 0)
                {
                    throw new ParseException("E0021");
                }
                else
                {
                    result.Positional.Add(arg);
                }

                ps.SkipInlineWs();
                ps.SkipIndent();

                if (ps.Current == ',')
                {
                    ps.Next();
                    ps.SkipInlineWs();
                    ps.SkipIndent();
                    continue;
                }
                else
                {
                    break;
                }
            }
            return result;
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

        Ast.StringLiteral _GetString(FtlParserStream ps)
        {
            var val = new StringBuilder();

            ps.ExpectChar('"');

            int ch;
            while ((ch = ps.TakeChar(x => x != '"' && x != '\r' && x != '\n')) != Eof)
            {
                if (ch == '\\')
                {
                    GetEscapeSequence(ps, new int[] { '{', '\\', '"' }, val);
                }
                else
                {
                    val.Append((char)ch);
                }
            }

            if (ps.CurrentIs('\r') || ps.CurrentIs('\n'))
            {
                throw new ParseException("E0020");
            }

            ps.Next();

            return new Ast.StringLiteral(val.ToString());
        }

        Ast.StringLiteral GetString(FtlParserStream ps) =>
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
                var id = GetIdentifier(ps);
                return new Ast.VariableReference(id);
            }

            if (ps.IsIdentifierStart())
            {
                var name = GetIdentifier(ps);
                return new Ast.MessageReference(name);
            }

            if (ps.IsNumberStart())
            {
                return GetNumber(ps);
            }

            if (ch == '-')
            {
                var id = GetTermIdentifier(ps);
                return new Ast.TermReference(id);
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
