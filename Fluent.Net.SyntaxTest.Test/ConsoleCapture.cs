using System;
using System.IO;

namespace Fluent.Net.SyntaxTest.Test
{
    public sealed class ConsoleCapture : IDisposable
    {
        private readonly StringWriter _output = new();
        private TextWriter _savedOutput;

        public ConsoleCapture()
        {
            _savedOutput = Console.Out;
            Console.SetOut(_output);
        }

        public void Dispose()
        {
            if (_savedOutput != null)
            {
                Console.SetOut(_savedOutput);
                _savedOutput = null;
            }
            _output.Dispose();
        }

        public string GetOutput()
        {
            return _output.ToString();
        }
    }
}
