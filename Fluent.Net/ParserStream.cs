using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fluent.Net
{
    public struct Position
    {
        /// <summary>
        /// Offset from the start of the stream (0 based)
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Line number (1 based)
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// Offset from the start of the line (1 based)
        /// </summary>
        public int LineOffset { get; set; }

        /// <summary>
        /// Construct a position at the start of a stream
        /// </summary>
        // public Position()
        // {
        //     Offset = 0;
        //     Line = 1; LineOffset = 1;
        // }

        /// <summary>
        /// Construct a position at the given location
        /// </summary>
        /// <param name="offset">The offset from the start of the stream (0 based)</param>
        /// <param name="line">The offset the line (1 based)</param>
        /// <param name="lineOffset">The offset from the start of the line (1 based)</param>
        public Position(int offset, int line, int lineOffset)
        {
            Offset = offset;
            Line = line;
            LineOffset = lineOffset;
        }

        static public Position Start = new Position(0, 1, 1);

        public override bool Equals(object obj)
        {
            if (obj is Position other)
            {
                return other.Offset == Offset && other.Line == Line
                    && other.LineOffset == LineOffset;
            }
            return false;
        }

        public override int GetHashCode()
        {
            int hashCode = Offset;
            hashCode = (hashCode * 397) ^ LineOffset;
            hashCode = (hashCode * 397) ^ Line;
            return hashCode;
        }

        public string FormatLineOffset()
        {
            return $"{Line}, {LineOffset}";
        }

        public override string ToString()
        {
            return $"{Offset}, {Line}, {LineOffset}";
        }
    }

    public class ParserStream
    {
        public const int Eof = -1;

        private readonly List<char> _buf = new List<char>();
        private readonly TextReader _input;
        private int _peekIndex = 0;
        private Position _position = Position.Start;
        private bool _inputEnd = false;
        private bool _peekEnd = false;
        private StringBuilder _captureBuf = null;

        public ParserStream(TextReader input)
        {
            _input = input;
            Current = input.Read();
        }

        public void BeginCapture()
        {
            if (_captureBuf == null)
            {
                _captureBuf = new StringBuilder();
            }
            else
            {
                _captureBuf.Clear();
            }
        }

        public void EndCapture()
        {
            _captureBuf = null;
        }

        public string GetCapturedText()
        {
            if (_captureBuf == null)
            {
                return "";
            }
            return _captureBuf.ToString();
        }

        public int Next()
        {
            if (_inputEnd)
            {
                return Eof;
            }

            if (_captureBuf != null)
            {
                _captureBuf.Append((char)Current);
            }

            if (Current == '\n')
            {
                _position.Line++;
                _position.LineOffset = 1;
            }
            else
            {
                _position.LineOffset++;
            }

            if (_buf.Count == 0)
            {
                Current = _input.Read();
            }
            else
            {
                Current = _buf[0];
                _buf.RemoveAt(0);
            }

            _position.Offset++;

            if (Current == Eof)
            {
                _inputEnd = true;
                _peekEnd = true;
            }

            _peekIndex = _position.Offset;
            return Current;
        }

        public int Current { get; private set; }

        public bool CurrentIs(int ch)
        {
            return Current == ch;
        }

        public string CurrentAsString()
        {
            int ch = Current;
            return ch == Eof ? "Eof" : ((char)ch).ToString();
        }

        public int CurrentPeek
        {
            get
            {
                if (_peekEnd)
                {
                    return Eof;
                }

                int diff = _peekIndex - _position.Offset;

                if (diff == 0)
                {
                    return Current;
                }
                return _buf[diff - 1];
            }
        }

        public bool CurrentPeekIs(int ch)
        {
            return CurrentPeek == ch;
        }

        public int Peek()
        {
            if (_peekEnd)
            {
                return Eof;
            }

            _peekIndex += 1;

            int diff = _peekIndex - _position.Offset;

            if (diff > _buf.Count)
            {
                int ch = _input.Read();
                if (ch != Eof)
                {
                    _buf.Add((char)ch);
                }
                else
                {
                    _peekEnd = true;
                    return Eof;
                }
            }

            return _buf[diff - 1];
        }

        public Position GetPosition()
        {
            return _position;
        }

        public int GetIndex()
        {
            return _position.Offset;
        }

        public int GetPeekIndex()
        {
            return _peekIndex;
        }

        public bool PeekCharIs(int ch)
        {
            if (_peekEnd)
            {
                return false;
            }

            int ret = Peek();

            _peekIndex -= 1;

            return ret == ch;
        }

        public void ResetPeek(int pos = 0)
        {
            if (pos != 0)
            {
                if (pos < _peekIndex)
                {
                    _peekEnd = false;
                }
                _peekIndex = pos;
            }
            else
            {
                _peekIndex = _position.Offset;
                _peekEnd = _inputEnd;
            }
        }

        public void SkipToPeek()
        {
            int diff = _peekIndex - _position.Offset;

            if (diff > 0)
            {
                if (_captureBuf != null)
                {
                    for (int i = 0; i < diff - 1; ++i)
                    {
                        _captureBuf.Append(_buf[i]);
                    }
                }

                Current = diff > _buf.Count ? Eof : _buf[diff - 1];
                _buf.RemoveRange(0, Math.Min(_buf.Count, diff));
            }

            _position.Offset = _peekIndex;
        }
    }
}
