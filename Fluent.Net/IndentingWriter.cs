using System;
using System.IO;

namespace Fluent.Net
{
    class IndentingWriter
    {
        private readonly TextWriter _output;
        private int _indents = 0;
        private bool _lastWasNL = true;

        public IndentingWriter(TextWriter output)
        {
            _output = output;
        }

        public void Indent()
        {
            ++_indents;
        }

        public void Dedent()
        {
            --_indents;
        }

        void WriteIndent()
        {
            if (_lastWasNL && _indents > 0)
            {
                _output.Write(new String(' ', _indents * 4));
            }
        }

        public void Write(string s)
        {
            for (int pos = 0; pos < s.Length;)
            {
                int end = s.IndexOf('\n', pos);
                if (end == -1)
                {
                    WriteIndent();
                    _output.Write(s.Substring(pos, s.Length - pos));
                    _lastWasNL = false;
                    break;
                }

                if (end > pos)
                {
                    WriteIndent();
                }
                _output.Write(s.Substring(pos, end - pos + 1));
                _lastWasNL = true;
                pos = end + 1;
            }
        }

        public void Write(char c)
        {
            if (c == '\n')
            {
                _lastWasNL = true;
            }
            else
            {
                WriteIndent();
                _lastWasNL = false;
            }
            _output.Write(c);
        }
    }
}
