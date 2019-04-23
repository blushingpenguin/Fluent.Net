using Fluent.Net.RuntimeAst;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fluent.Net
{
    struct VariantResult
    {
        public IList<Variant> Variants;
        public int? DefaultIndex;
    }

    /// <summary>
    /// The `Parser` class is responsible for parsing FTL resources.
    /// 
    /// It's only public method is `getResource(source)` which takes an FTL string
    /// and returns a two element Array with an Object of entries generated from the
    /// source as the first element and an array of ParseException objects as the
    /// second.
    /// 
    /// This parser is optimized for runtime performance.
    /// 
    /// There is an equivalent of this parser in syntax/parser which is
    /// generating full AST which is useful for FTL tools.
    /// </summary>
    public class RuntimeParser
    {
        const int MAX_PLACEABLES = 100;
        const int Eof = ParserStream.Eof;

        FtlParserStream _stream;
        Dictionary<string, Message> _entries;

        int CurrentPeek { get => _stream.CurrentPeek; } 
        int Current { get => _stream.Current; } 
        bool CurrentPeekIs(int ch) => _stream.CurrentPeekIs(ch);
        string CurrentAsString() => _stream.CurrentAsString();
        int Next() => _stream.Next();
        int Peek() => _stream.Peek();
        void SkipInlineWs() => _stream.SkipInlineWs();
        void SkipBlankLines() => _stream.SkipBlankLines();

        public class Result
        {
            public IDictionary<string, Message> Entries { get; set; }
            public IList<ParseException> Errors { get; set; }
        }

        /// <summary>
        /// Parse FTL code into entries formattable by the MessageContext.
        /// 
        /// Given a string of FTL syntax, return a map of entries that can be passed
        /// to MessageContext.format and a list of errors encountered during parsing.
        /// </summary>
        /// <param name='ftl'>The ftl text</param>
        /// @returns {Array<Object, Array>}
        public Result GetResource(TextReader input)
        {
            _stream = new FtlParserStream(input);
            _entries = new Dictionary<string, Message>();
            var errors = new List<ParseException>();

            SkipWs();
            while (Current != Eof)
            {
                try
                {
                    GetEntry();
                }
                catch (ParseException e)
                {
                    errors.Add(e);
                    _stream.SkipToNextEntryStart(true);
                }
                SkipWs();
            }
            return new Result()
            {
                Entries = _entries,
                Errors = errors
            };
        }

        /// <summary>
        /// Parse the source string from the current index as an FTL entry
        /// and add it to object's entries property.
        /// </summary>
        void GetEntry()
        {
            // The index here should either be at the beginning of the file
            // or right after new line.
            // TODO:
            // if (_index != 0 && _source[_index - 1] != '\n')
            // {
            //     Error("Expected an entry to start " +
            //         " at the beginning of the file or on a new line.");
            // }

            // We don't care about comments or sections at runtime
            int ch = Current;
            int ch2 = Peek();
            if (ch == '#' && (ch2 == '#' || ch2 == ' ' ||
                              ch2 == '\r' || ch2 == '\n'))
            {
                SkipComment();
                return;
            }

            GetMessage();
        }

        /// <summary>
        /// Parse the source string from the current index as an FTL message
        /// and add it to the entries property on the Parser.
        /// </summary>
        void GetMessage()
        {
            var id = GetEntryIdentifier();

            SkipInlineWs();

            if (Current == '=')
            {
                Next();
            }
            else
            {
                Error("Expected \"=\" after the identifier");
            }

            SkipInlineWs();

            var val = GetPattern();

            if (id.StartsWith("-") && val == null)
            {
                Error("Expected term to have a value");
            }

            IDictionary<string, Node> attrs = null;

            if (Current == ' ')
            {
                _stream.PeekInlineWs();
                if (_stream.CurrentPeekIs('.'))
                {
                    attrs = GetAttributes();
                }
            }

            if (val == null && attrs == null)
            {
                Error("Expected message to have a value or attributes");
            }

            _entries[id] = new Message()
            {
                Attributes = attrs,
                Value = val
            };
        }

        /// <summary>
        /// Skip whitespace.
        /// </summary>
        void SkipWs()
        {
            while (Current == ' ' || Current == '\n' ||
                   Current == '\t' || Current == '\r')
            {
                Next();
            }
        }

        void PeekWs()
        {
            while (CurrentPeek == ' '  || CurrentPeek == '\n' ||
                   CurrentPeek == '\t' || CurrentPeek == '\r')
            {
                Peek();
            }
        }

        string GetIdentifier(bool allowTerm = false)
        {
            var name = new StringBuilder();
            if (allowTerm && Current == '-')
            {
                name.Append('-');
                Next();
            }
            else
            {
                name.Append((char)_stream.TakeIDStart());
            }

            int ch;
            while ((ch = _stream.TakeIDChar()) != Eof)
            {
                name.Append((char)ch);
            }

            return name.ToString();
        }

        /// <summary>
        /// Get identifier of a Message or a Term (staring with a dash).
        /// </summary>
        string GetEntryIdentifier()
        {
            return GetIdentifier(true);
        }

        bool IsVariantLeader(int c)
        {
            return
                (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                c == '_';
        }

        bool IsVariantChar(int c)
        {
            return
                IsVariantLeader(c) ||
                (c >= '0' && c <= '9') ||
                c == '-' || c == '_';
        }

        /// <summary>
        /// Get Variant name.
        /// </summary>
        Node GetVariantName()
        {
            if (!IsVariantLeader(Current) && Current != ' ')
            {
                Error("Expected a keyword (starting with [a-zA-Z_ ])");
            }

            var name = new StringBuilder();

            for (; ; )
            {
                // if we've got a space, peek ahead and check there's a valid
                // variant name character after it -- the keyword can't end
                // with a space
                if (Current == ' ')
                {
                    int spaces = 1;
                    for (; Peek() == ' '; ++spaces)
                    {
                    }
                    if (!IsVariantChar(CurrentPeek))
                    {
                        break;
                    }
                    name.Append(' ', spaces);
                    _stream.SkipToPeek();
                }
                else if (IsVariantChar(Current))
                {
                    name.Append((char)Current);
                    Next();
                }
                else
                {
                    break;
                }
            }

            return new VariantName { Name = name.ToString() };
        }

        /// <summary>
        /// Get simple string argument enclosed in `'`.
        /// </summary>
        Node GetString()
        {
            Next(); // "
            var result = new StringBuilder();

            for (; ;)
            {
                if (Current == '"')
                {
                    Next(); // "
                    break;
                }
                if (Current == '\r' || Current == '\n' || Current == Eof)
                {
                    Error("Unterminated string expression");
                }
                if (Current == '\\')
                {
                    GetEscapedCharacter(result, new int[] { '{', '\\', '"' });
                }
                else
                {
                    result.Append((char)Current);
                    Next();
                }
            }

            return new StringLiteral() { Value = result.ToString() };
        }

        static StringLiteral StringLiteralFromBuffer(StringBuilder buf)
        {
            return buf.Length > 0 ?
                new StringLiteral()
                {
                    Value = Parser.TrimRight(buf.ToString())
                } : null;
        }

        /// <summary>
        /// Parses a Message pattern.
        /// Message Pattern may be a simple string or an array of strings
        /// and placeable expressions.
        /// </summary>
        Node GetPattern()
        {
            // We're going to first try to see if the pattern is simple.
            // If it is we can just look for the end of the line and read the string.
            //
            // Then, if either the line contains a placeable opening `{` or the
            // next line starts an indentation, we switch to complex pattern.
            var firstLineContent = new StringBuilder();
            for (; CurrentPeek != '\r' && CurrentPeek != '\n' &&
                   CurrentPeek != '\\' && CurrentPeek != '{'  &&
                   CurrentPeek != Eof; _stream.Peek())
            {
                firstLineContent.Append((char)CurrentPeek);
            }

            if (CurrentPeek == '{' || CurrentPeek == '\\')
            {
                _stream.ResetPeek();
                return GetComplexPattern();
            }

            _stream.PeekBlankLines();
            if (CurrentPeek != ' ')
            {
                // No indentation means we're done with this message. Callers should check
                // if the return value here is null. It may be OK for messages, but not OK
                // for terms, attributes and variants.
                _stream.SkipToPeek(); // consume the line
                return StringLiteralFromBuffer(firstLineContent);
            }

            int lineStart = _stream.GetPeekIndex();
            _stream.PeekInlineWs();
            if (CurrentPeek == '.')
            {
                // The pattern is followed by an attribute. Rewind _index to the first
                // column of the current line as expected by GetAttributes.
                _stream.ResetPeek(lineStart);
                _stream.SkipToPeek();
                return StringLiteralFromBuffer(firstLineContent);
            }

            // It's a multiline pattern which started on the same line as the
            // identifier. Reparse the whole pattern to make sure we get all of it.
            if (firstLineContent.Length > 0)
            {
                _stream.ResetPeek();
            }
            // Otherwise parse up from here on in (having skipped inline ws)
            else
            {
                _stream.SkipToPeek();
            }
            return GetComplexPattern();
        }

        /// <summary>
        /// Parses a complex Message pattern.
        /// This function is called by GetPattern when the message is multiline,
        /// or contains escape chars or placeables.
        /// It does full parsing of complex patterns.
        /// </summary>
        Node GetComplexPattern()
        {
            var buffer = new StringBuilder();
            var content = new List<Node>();
            int placeables = 0;

            while (Current != Eof)
            {
                // This block handles multi-line strings combining strings separated
                // by new line.
                if (Current == '\r' || Current == '\n')
                {
                    if (Current == '\r' && Peek() == '\n')
                    {
                        Next();
                    }
                    Next();

                    // We want to capture the start and end pointers
                    // around blank lines and add them to the buffer
                    // but only if the blank lines are in the middle
                    // of the string.
                    _stream.BeginCapture();
                    _stream.SkipBlankLines();

                    if (Current != ' ')
                    {
                        _stream.EndCapture();
                        break;
                    }
                    _stream.PeekInlineWs();

                    if (CurrentPeek == '}' || CurrentPeek == '[' ||
                        CurrentPeek == '*' || CurrentPeek == '.')
                    {
                        _stream.EndCapture();
                        _stream.ResetPeek();
                        break;
                    }
                    buffer.Append(_stream.GetCapturedText());
                    _stream.EndCapture();
                    _stream.SkipToPeek();

                    if (buffer.Length > 0 || content.Count > 0)
                    {
                        buffer.Append('\n');
                    }
                    continue;
                }
                // check if it's a valid escaped thing
                if (Current == '\\') 
                {
                    GetEscapedCharacter(buffer, new int[] { '{', '\\' });
                    continue;
                }
                if (Current == '{')
                {
                    // Push the buffer to content array right before placeable
                    if (buffer.Length > 0)
                    {
                        content.Add(new StringLiteral() { Value = buffer.ToString() });
                    }
                    if (placeables > MAX_PLACEABLES - 1)
                    {
                        Error(
                            $"Too many placeables, maximum allowed is {MAX_PLACEABLES}");
                    }
                    buffer.Length = 0;
                    content.Add(GetPlaceable());

                    Next();
                    placeables++;
                    continue;
                }

                buffer.Append((char)Current);
                Next();
            }

            StringLiteral extra = null;
            if (buffer.Length > 0)
            {
                extra = new StringLiteral()
                { 
                    Value = Parser.TrimRight(buffer.ToString()) 
                };
            }
            if (content.Count == 0)
            {
                return extra;
            }
            if (extra != null)
            {
                content.Add(extra);
            }
            return new Pattern() { Elements = content };
        }

        bool TryParseHex(string s, out int val)
        {
            val = 0;
            if (s == null || s.Length == 0)
            {
                return false;
            }
            for (int i = 0; i < s.Length; ++i)
            {
                char c = s[i];
                int digit = (c >= 'A' && c <= 'F') ? c - 'A' :
                            (c >= 'a' && c <= 'f') ? c - 'a' :
                            (c >= '0' && c <= '9') ? c - '0' : -1;
                if (digit < 0 || // not hex
                    val > Int32.MaxValue / 16 || // overflow
                    (val == Int32.MaxValue / 16 && // boundary overflow
                     digit > (Int32.MaxValue - 16 * (Int32.MaxValue / 16))))
                {
                    return false;
                }
                val = val * 16 + digit;
            }
            return true;
        }

        /// <summary>
        /// Parse an escape sequence and return the unescaped character.
        /// </summary>
        void GetEscapedCharacter(StringBuilder buffer, int[] specials)
        {
            int ch = Next();

            if (Array.IndexOf(specials, ch) >= 0)
            {
                Next();
                buffer.Append((char)ch);
                return;
            }
        
            if (ch == 'u')
            {
                Next();
                char[] sequence = new char[4];
                int i = 0;
                for (; i < 4; ++i)
                {
                    ch = _stream.TakeHexDigit();
                    if (ch == Eof)
                    {
                        break;
                    }
                    sequence[i] = (char)ch;
                }
                var charCodeString = new String(sequence, 0, i);
                int charCodeVal;
                if (i != 4 || !TryParseHex(charCodeString, out charCodeVal))
                {
                    throw Error($"Invalid Unicode escape sequence: \\u{charCodeString}");
                }
                buffer.Append(Char.ConvertFromUtf32(charCodeVal));
                return;
            }

            Error($"Unknown escape sequence: \\{CurrentAsString()}");
        }

        /// <summary>
        /// Parses a single placeable in a Message pattern and returns its
        /// expression.
        /// </summary>
        Node GetPlaceable()
        {
            Next();
            PeekWs();
            int peekIndex = _stream.GetPeekIndex();
            if (CurrentPeekIs('*') ||
                CurrentPeekIs('[') && Peek() != ']')
            {
                _stream.ResetPeek(peekIndex);
                _stream.SkipToPeek();
                
                var variantResult = GetVariants();
                return new SelectExpression()
                {
                    Variants = variantResult.Variants,
                    DefaultIndex = variantResult.DefaultIndex
                };
            }

            // Rewind the index and only support in-line white-space now.
            _stream.SkipInlineWs();

            var selector = GetSelectorExpression();

            SkipWs();

            if (Current == '}')
            {
                if (selector is GetAttribute ae2 && ae2.Id.Name.StartsWith("-"))
                {
                    Error(
                        "Attributes of private messages cannot be interpolated.");
                }

                return selector;
            }

            if (Current != '-' || Peek() != '>')
            {
                Error("Expected '}' or '->'");
            }

            if (selector is MessageReference)
            {
                Error("Message references cannot be used as selectors.");
            }

            if (selector is GetVariant)
            {
                Error("Variants cannot be used as selectors.");
            }

            if (selector is GetAttribute ae && !ae.Id.Name.StartsWith("-"))
            {
                Error(
                    "Attributes of public messages cannot be used as selectors."
                );
            }

            Next();
            Next(); // ->

            SkipInlineWs();

            if (Current != '\r' && Current != '\n')
            {
                Error("Variants should be listed in a new line");
            }

            SkipWs();

            var variants = GetVariants();
            if (variants.Variants.Count == 0)
            {
                Error("Expected members for the select expression");
            }

            return new SelectExpression()
            {
                Expression = selector,
                Variants = variants.Variants,
                DefaultIndex = variants.DefaultIndex
            };
        }

        /// <summary>
        /// Parses a selector expression.
        /// </summary>
        Node GetSelectorExpression()
        {
            if (Current == '{')
            {
                return GetPlaceable();
            }

            Node literal = GetLiteral();
            if (!(literal is MessageReference))
            {
                return literal;
            }
            var messageReference = (MessageReference)literal;

            if (Current == '.')
            {
                Next();
                var name = GetIdentifier();
                Next();
                return new GetAttribute() 
                {
                    Id = messageReference,
                    Name = name 
                };
            }

            if (Current == '[')
            {
                Next();
                var key = GetVariantKey();
                Next();
                return new GetVariant() { Id = messageReference, Key = key };
            }

            if (Current == '(')
            {
                Next();
                var args = GetCallArgs();

                if (!Parser.s_fnName.IsMatch(messageReference.Name))
                {
                    Error("Function names must be all upper-case");
                }

                Next();
                return new CallExpression() { Function = messageReference.Name, Args = args };
            }

            return literal;
        }

        /// <summary>
        /// Parses call arguments for a CallExpression.
        /// </summary>
        List<Node> GetCallArgs()
        {
            var args = new List<Node>();

            for (; ;)
            {
                SkipWs();

                if (Current == ')')
                {
                    break;
                }

                var exp = GetSelectorExpression();

                // MessageReference in this place may be an entity reference, like:
                // `call(foo)`, or, if it's followed by `:` it will be a key-value pair.
                if (!(exp is MessageReference))
                {
                    args.Add(exp);
                }
                else
                {
                    _stream.PeekInlineWs();

                    if (Current == ':')
                    {
                        Next();
                        SkipWs();

                        var val = GetSelectorExpression();

                        // If the expression returned as a value of the argument
                        // is not a quote delimited string or number, throw.
                        //
                        // We don't have to check here if the pattern is quote delimited
                        // because that's the only type of string allowed in expressions.
                        if (val is StringLiteral ||
                            val is NumberLiteral ||
                            val is Pattern)
                        {
                            args.Add(new NamedArgument()
                            {
                                Name = ((MessageReference)exp).Name,
                                Value = val
                            });

                        }
                        else
                        {
                            // XXX: can't seek backwards
                            // do we need PeekSelectorExpression or can we leave without this?
                            // _index = _source.lastIndexOf(':', _index) + 1;
                            Error("Expected string in quotes, number.");
                        }

                    }
                    else
                    {
                        args.Add(exp);
                    }
                }

                SkipWs();

                if (Current == ')')
                {
                    break;
                }
                if (Current == ',')
                {
                    Next();
                }
                else
                {
                    Error("Expected ',' or ')'");
                }
            }
            return args;
        }

        /// <summary>
        /// Parses an FTL Number.
        /// </summary>
        /// 
        /// @returns {Object}
        Node GetNumber()
        {
            var num = new StringBuilder();

            // The number literal may start with negative sign `-`.
            if (Current == '-')
            {
                num.Append('-');
                Next();
            }

            // next, we expect at least one digit
            if (Current < '0' || Current > '9')
            {
                Error($"Unknown literal '{CurrentAsString()}'");
            }

            // followed by potentially more digits
            do
            {
                num.Append((char)Current);
                Next();
            }
            while (Current >= '0' && Current <= '9');

            // followed by an optional decimal separator `.`
            if (Current == '.')
            {
                num.Append('.');
                Next();

                // followed by at least one digit
                if (Current < '0' || Current > '9')
                {
                    Error($"Unknown literal '{CurrentAsString()}'");
                }

                // and optionally more digits
                while (Current >= '0' && Current <= '9')
                {
                    num.Append((char)Current);
                    Next();
                }
            }

            return new NumberLiteral() { Value = num.ToString() };
        }

        /// <summary>
        /// Parses a list of Message attributes.
        /// </summary>
        IDictionary<string, Node> GetAttributes()
        {
            var attrs = new Dictionary<string, Node>();

            while (Current != Eof)
            {
                if (Current != ' ')
                {
                    break;
                }
                SkipInlineWs();

                if (Current != '.')
                {
                    break;
                }
                Next();

                var key = GetIdentifier();

                SkipInlineWs();
                
                if (Current != '=')
                {
                    Error("Expected '='");
                }
                Next();

                SkipInlineWs();

                var val = GetPattern();

                if (val == null)
                {
                    Error("Expected attribute to have a value");
                }

                attrs[key] = val;

                SkipBlankLines();
            }

            return attrs;
        }

        /// <summary>
        /// Parses a list of Selector variants.
        /// </summary>
        VariantResult GetVariants()
        {
            var result = new VariantResult()
            {
                Variants = new List<Variant>()
            };

            while (Current != Eof)
            {
                if ((Current != '[' || Peek() == '[') &&
                    Current != '*')
                {
                    break;
                }
                if (Current == '*')
                {
                    Next();
                    result.DefaultIndex = result.Variants.Count;
                }

                if (Current != '[')
                {
                    Error("Expected '['");
                }
                Next();

                var key = GetVariantKey();

                SkipInlineWs();

                var val = GetPattern();

                if (val == null)
                {
                    Error("Expected variant to have a value");
                }

                result.Variants.Add(new Variant() { Key = key, Value = val });

                SkipWs();
            }

            return result;
        }

        /// <summary>
        /// Parses a Variant key.
        /// </summary>
        Node GetVariantKey()
        {
            // VariantKey may be a Keyword or Number
            Node literal;
            if ((Current >= '0' && Current <= '9') || Current == '-')
            {
                literal = GetNumber();
            }
            else
            {
                literal = GetVariantName();
            }

            if (Current != ']')
            {
                Error("Expected ']'");
            }
            Next();

            return literal;
        }

        /// <summary>
        /// Parses an FTL literal.
        /// </summary>
        Node GetLiteral()
        {
            if (Current == '$')
            {
                Next();
                var name = GetIdentifier();
                return new VariableReference() { Name = name };
            }

            int ch = Current;
            if (ch == '-')
            {
                ch = Peek();
            }

            if (FtlParserStream.IsCharIDStart(ch))
            {
                string name = GetEntryIdentifier();
                return new MessageReference { Name = name };
            }

            if (FtlParserStream.IsDigit(ch))
            {
                return GetNumber();
            }

            if (Current == '"')
            {
                return GetString();
            }

            // the compiler can't see that Error always throws
            throw Error("Expected literal");
        }

        /// <summary>
        /// Skips an FTL comment.
        /// </summary>
        void SkipComment()
        {
            // At runtime, we don't care about comments so we just have
            // to parse them properly and skip their content.
            while (Current != Eof)
            {
                if (Current == '\r' || Current == '\n')
                {
                    _stream.SkipNewLine();

                    int next = Peek();
                    if (Current == '#' && (next == ' ' || next == '#'))
                    {
                        Next();
                        Next();
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    Next();
                }
            }
        }

        /// <summary>
        /// Throws a ParseException with a given message.
        /// </summary>
        /// <param name="message">The error message</param>
        static Exception Error(string message)
        {
            throw new ParseException(message);
        }
    }
}
