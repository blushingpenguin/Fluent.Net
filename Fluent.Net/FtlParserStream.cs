using System;
using System.IO;

namespace Fluent.Net
{
    public class FtlParserStream : ParserStream
    {
        public FtlParserStream(TextReader input) :
            base(input)
        {
        }

        public static bool IsInlineWs(int c)
        {
            return c == ' ' || c == '\t';
        }

        public void SkipInlineWs()
        {
            while (IsInlineWs(Current))
            {
                Next();
            }
        }

        public void PeekInlineWs()
        {
            for (int ch = CurrentPeek; IsInlineWs(ch); ch = Peek())
            {
            }
        }

        public void SkipBlankLines()
        {
            while (true)
            {
                PeekInlineWs();

                if (CurrentPeekIs('\r') || CurrentPeekIs('\n'))
                {
                    Next();
                }
                else
                {
                    ResetPeek();
                    break;
                }
            }
        }

        public void PeekBlankLines()
        {
            while (true)
            {
                int lineStart = GetPeekIndex();

                PeekInlineWs();

                if (CurrentPeekIs('\r') || CurrentPeekIs('\n'))
                {
                    Peek();
                }
                else
                {
                    ResetPeek(lineStart);
                    break;
                }
            }
        }

        public void SkipIndent()
        {
            SkipBlankLines();
            SkipInlineWs();
        }

        public void ExpectChar(int ch)
        {
            if (Current == ch)
            {
                Next();
                return;
            }

            if (ch == '\n')
            {
                throw new InvalidOperationException("Internal logic error - ExpectChar('\n') should be ExpectNewLine()");
            }

            throw new ParseException("E0003",
                ch == Eof ? "Eof" : ((char)ch).ToString());
        }

        public void ExpectNewLine()
        {
            if (Current == '\r')
            {
                Next();
                if (Current == '\n')
                {
                    Next();
                }
                return;
            }
            if (Current == '\n')
            {
                Next();
                return;
            }
            // Unicode Character 'SYMBOL FOR NEWLINE' (U+2424)
            throw new ParseException("E0003", "\u2424");
        }

        public void ExpectIndent()
        {
            ExpectNewLine();
            SkipBlankLines();
            ExpectChar(' ');
            SkipInlineWs();
        }

        public bool TakeCharIf(int ch)
        {
            if (Current == ch)
            {
                Next();
                return true;
            }
            return false;
        }

        public int TakeChar(Func<int, bool> f)
        {
            int ch = Current;
            if (ch != Eof && f(ch))
            {
                Next();
                return ch;
            }
            return Eof;
        }

        public static bool IsCharIDStart(int ch)
        {
            return (ch >= 'a' && ch <= 'z') ||
                   (ch >= 'A' && ch <= 'Z');
        }

        public bool IsEntryIDStart()
        {
            if (CurrentIs('-'))
            {
                Peek();
            }

            int ch = CurrentPeek;
            bool isID = IsCharIDStart(ch);
            ResetPeek();
            return isID;
        }

        public bool IsNumberStart()
        {
            if (CurrentIs('-'))
            {
                Peek();
            }

            int ch = CurrentPeek;
            bool isDigit = ch >= '0' && ch <= '9';
            ResetPeek();
            return isDigit;
        }

        public void SkipNewLine()
        {
            if (Current == '\r')
            {
                Next();
            }
            if (Current == '\n')
            {
                Next();
            }
        }  

        public bool IsPeekNewLine()
        {
            if (CurrentPeekIs('\r'))
            {
                int peekIndex = GetPeekIndex();
                if (Peek() != '\n')
                {
                    ResetPeek(peekIndex);
                }
                return true;
            }
            return CurrentPeekIs('\n');
        }

        public static bool IsCharPatternContinuation(int ch)
        {
            return ch != Eof && ch != '}' && ch != '.'
                && ch != '[' && ch != '*';
        }

        public bool IsPeekPatternStart()
        {
            PeekInlineWs();
            int ch = CurrentPeek;

            // Inline Patterns may start with any char.
            if (ch != Eof && !IsPeekNewLine())
            {
                return true;
            }

            return IsPeekNextLinePatternStart();
        }

        public bool IsPeekNextLineZeroFourStyleComment()
        {
            if (!IsPeekNewLine())
            {
                return false;
            }

            Peek();

            if (CurrentPeekIs('/'))
            {
                Peek();
                if (CurrentPeekIs('/'))
                {
                    ResetPeek();
                    return true;
                }
            }

            ResetPeek();
            return false;
        }

        // -1 - any
        //  0 - comment
        //  1 - group comment
        //  2 - resource comment
        public bool IsPeekNextLineComment(int level = -1)
        {
            if (!IsPeekNewLine())
            {
                return false;
            }

            int i = 0;

            while (i <= level || (level == -1 && i < 3))
            {
                Peek();
                if (!CurrentPeekIs('#'))
                {
                    if (i != level && level != -1)
                    {
                        ResetPeek();
                        return false;
                    }
                    break;
                }
                i++;
            }

            Peek();
            int ch = CurrentPeek;
            if (ch == ' ' || ch == '\n' || ch == '\r')
            {
                ResetPeek();
                return true;
            }

            ResetPeek();
            return false;
        }

        public bool IsPeekNextLineVariantStart()
        {
            if (!IsPeekNewLine())
            {
                return false;
            }

            Peek();

            PeekBlankLines();

            int ptr = GetPeekIndex();

            PeekInlineWs();

            if (GetPeekIndex() - ptr == 0)
            {
                ResetPeek();
                return false;
            }

            if (CurrentPeekIs('*'))
            {
                Peek();
            }

            if (CurrentPeekIs('[') && !PeekCharIs('['))
            {
                ResetPeek();
                return true;
            }
            ResetPeek();
            return false;
        }

        public bool IsPeekNextLineAttributeStart()
        {
            if (!IsPeekNewLine())
            {
                return false;
            }

            Peek();

            PeekBlankLines();

            int ptr = GetPeekIndex();

            PeekInlineWs();

            if (GetPeekIndex() - ptr == 0)
            {
                ResetPeek();
                return false;
            }

            if (CurrentPeekIs('.'))
            {
                ResetPeek();
                return true;
            }

            ResetPeek();
            return false;
        }

        public bool IsPeekNextLinePatternStart()
        {
            if (!IsPeekNewLine())
            {
                return false;
            }

            Peek();

            PeekBlankLines();

            int ptr = GetPeekIndex();

            PeekInlineWs();

            if (GetPeekIndex() - ptr == 0)
            {
                ResetPeek();
                return false;
            }

            if (!IsCharPatternContinuation(CurrentPeek))
            {
                ResetPeek();
                return false;
            }

            ResetPeek();
            return true;
        }

        public void SkipToNextEntryStart()
        {
            while (Current != Eof)
            {
                // if (CurrentIs('\n') && !PeekCharIs('\n'))
                bool isCR = Current == '\r';
                if (isCR || Current == '\n')
                {
                    Next();
                    if (isCR)
                    {
                        if (Current == '\n')
                        {
                            Next();
                        }
                    }
                    if (Current != '\r' && Current != '\n')
                    {
                        if (Current == Eof ||
                            IsEntryIDStart() ||
                            CurrentIs('#') ||
                            (CurrentIs('/') && PeekCharIs('/')) ||
                            (CurrentIs('[') && PeekCharIs('[')))
                        {
                            break;
                        }
                    }
                }
                else
                {
                    Next();
                }
            }
        }

        public int TakeIDStart(bool allowTerm)
        {
            if (allowTerm && CurrentIs('-'))
            {
                Next();
                return '-';
            }

            if (IsCharIDStart(Current))
            {
                int ret = Current;
                Next();
                return ret;
            }

            string allowedRange = allowTerm ? "a-zA-Z-" : "a-zA-Z";
            throw new ParseException("E0004", allowedRange);
        }

        public static bool IsIdChar(int ch)
        {
            return (ch >= 'a' && ch <= 'z') ||
                   (ch >= 'A' && ch <= 'Z') ||
                   (ch >= '0' && ch <= '9') ||
                    ch == '_' || ch == '-';
        }

        public int TakeIDChar()
        {
            return TakeChar(IsIdChar);
        }

        public static bool IsVariantNameChar(int ch)
        {
            return (ch >= 'a' && ch <= 'z') ||
                   (ch >= 'A' && ch <= 'Z') ||
                   (ch >= '0' && ch <= '9') ||
                    ch == '_' || ch == '-' || ch == ' ';
        }

        public int TakeVariantNameChar()
        {
            return TakeChar(IsVariantNameChar);
        }

        public static bool IsDigit(int ch)
        {
            return ch >= '0' && ch <= '9';
        }

        public int TakeDigit()
        {
            return TakeChar(IsDigit);
        }
    }
}
