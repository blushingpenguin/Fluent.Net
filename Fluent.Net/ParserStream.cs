using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fluent.Net
{
    public class ParserStream
    {
        public const int Eof = -1;

        int _peekIndex = 0;
        int _index = 0;
        List<char> _buf = new List<char>();
        TextReader _input;
        bool _inputEnd = false;
        bool _peekEnd = false;
        StringBuilder _captureBuf = null;

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

        private int Read()
        {
            int ch = _input.Read();
            if (ch != Eof && _captureBuf != null)
            {
                _captureBuf.Append((char)ch);
            }
            return ch;
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

            if (_buf.Count == 0)
            {
                Current = _input.Read();
            }
            else
            {
                Current = _buf[0];
                _buf.RemoveAt(0);
            }

            _index++;

            if (Current == Eof)
            {
                _inputEnd = true;
                _peekEnd = true;
            }

            _peekIndex = _index;
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

                int diff = _peekIndex - _index;

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

            int diff = _peekIndex - _index;

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

        public int GetIndex()
        {
            return _index;
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
                _peekIndex = _index;
                _peekEnd = _inputEnd;
            }
        }

        public void SkipToPeek()
        {
            int diff = _peekIndex - _index;

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

            _index = _peekIndex;
        }
    }
}
