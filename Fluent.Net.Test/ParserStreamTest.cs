using FluentAssertions;
using NUnit.Framework;
using System.IO;

namespace Fluent.Net.Test
{
    public class ParserStreamTest
    {
        [Test]
        public void Next()
        {
            using var sr = new StringReader("abcd");
            var ps = new ParserStream(sr);

            ps.Current.Should().Be('a');
            ps.GetIndex().Should().Be(0);

            ps.Next().Should().Be('b');
            ps.Current.Should().Be('b');
            ps.GetIndex().Should().Be(1);

            ps.Next().Should().Be('c');
            ps.Current.Should().Be('c');
            ps.GetIndex().Should().Be(2);

            ps.Next().Should().Be('d');
            ps.Current.Should().Be('d');
            ps.GetIndex().Should().Be(3);

            ps.Next().Should().Be(ParserStream.Eof);
            ps.Current.Should().Be(ParserStream.Eof);
            ps.GetIndex().Should().Be(4);
        }

        [Test]
        public void Peek()
        {
            using var sr = new StringReader("abcd");
            var ps = new ParserStream(sr);

            ps.CurrentPeek.Should().Be('a');
            ps.GetPeekIndex().Should().Be(0);

            ps.Peek().Should().Be('b');
            ps.CurrentPeek.Should().Be('b');
            ps.GetPeekIndex().Should().Be(1);

            ps.Peek().Should().Be('c');
            ps.CurrentPeek.Should().Be('c');
            ps.GetPeekIndex().Should().Be(2);

            ps.Peek().Should().Be('d');
            ps.CurrentPeek.Should().Be('d');
            ps.GetPeekIndex().Should().Be(3);

            ps.Peek().Should().Be(ParserStream.Eof);
            ps.CurrentPeek.Should().Be(ParserStream.Eof);
            ps.GetPeekIndex().Should().Be(4);
        }

        [Test]
        public void PeekAndNext()
        {
            using var sr = new StringReader("abcd");
            var ps = new ParserStream(sr);

            ps.Peek().Should().Be('b');
            ps.GetPeekIndex().Should().Be(1);
            ps.GetIndex().Should().Be(0);

            ps.Next().Should().Be('b');
            ps.GetPeekIndex().Should().Be(1);
            ps.GetIndex().Should().Be(1);

            ps.Peek().Should().Be('c');
            ps.GetPeekIndex().Should().Be(2);
            ps.GetIndex().Should().Be(1);

            ps.Next().Should().Be('c');
            ps.GetPeekIndex().Should().Be(2);
            ps.GetIndex().Should().Be(2);
            ps.Current.Should().Be('c');
            ps.CurrentPeek.Should().Be('c');

            ps.Peek().Should().Be('d');
            ps.GetPeekIndex().Should().Be(3);
            ps.GetIndex().Should().Be(2);

            ps.Next().Should().Be('d');
            ps.GetPeekIndex().Should().Be(3);
            ps.GetIndex().Should().Be(3);
            ps.Current.Should().Be('d');
            ps.CurrentPeek.Should().Be('d');

            ps.Peek().Should().Be(ParserStream.Eof);
            ps.GetPeekIndex().Should().Be(4);
            ps.GetIndex().Should().Be(3);
            ps.Current.Should().Be('d');
            ps.CurrentPeek.Should().Be(ParserStream.Eof);

            ps.Peek().Should().Be(ParserStream.Eof);
            ps.GetPeekIndex().Should().Be(4);
            ps.GetIndex().Should().Be(3);

            ps.Next().Should().Be(ParserStream.Eof);
            ps.GetPeekIndex().Should().Be(4);
            ps.GetIndex().Should().Be(4);
        }

        [Test]
        public void SkipToPeek()
        {
            using var sr = new StringReader("abcd");
            var ps = new ParserStream(sr);

            ps.Peek();
            ps.Peek();

            ps.SkipToPeek();

            ps.Current.Should().Be('c');
            ps.CurrentPeek.Should().Be('c');
            ps.GetPeekIndex().Should().Be(2);
            ps.GetIndex().Should().Be(2);

            ps.Peek();

            ps.Current.Should().Be('c');
            ps.CurrentPeek.Should().Be('d');
            ps.GetPeekIndex().Should().Be(3);
            ps.GetIndex().Should().Be(2);

            ps.Next();

            ps.Current.Should().Be('d');
            ps.CurrentPeek.Should().Be('d');
            ps.GetPeekIndex().Should().Be(3);
            ps.GetIndex().Should().Be(3);
        }

        [Test]
        public void ResetPeek()
        {
            using var sr = new StringReader("abcd");
            var ps = new ParserStream(sr);

            ps.Next();
            ps.Peek();
            ps.Peek();
            ps.ResetPeek();

            ps.Current.Should().Be('b');
            ps.CurrentPeek.Should().Be('b');
            ps.GetPeekIndex().Should().Be(1);
            ps.GetIndex().Should().Be(1);

            ps.Peek();

            ps.Current.Should().Be('b');
            ps.CurrentPeek.Should().Be('c');
            ps.GetPeekIndex().Should().Be(2);
            ps.GetIndex().Should().Be(1);

            ps.Peek();
            ps.Peek();
            ps.Peek();
            ps.ResetPeek();

            ps.Current.Should().Be('b');
            ps.CurrentPeek.Should().Be('b');
            ps.GetPeekIndex().Should().Be(1);
            ps.GetIndex().Should().Be(1);

            ps.Peek().Should().Be('c');
            ps.Current.Should().Be('b');
            ps.CurrentPeek.Should().Be('c');
            ps.GetPeekIndex().Should().Be(2);
            ps.GetIndex().Should().Be(1);

            ps.Peek().Should().Be('d');
            ps.Peek().Should().Be(ParserStream.Eof);
        }

        [Test]
        public void PeekCharIs()
        {
            using var sr = new StringReader("abcd");
            var ps = new ParserStream(sr);

            ps.Next();
            ps.Peek();

            ps.PeekCharIs('d').Should().BeTrue();

            ps.Current.Should().Be('b');
            ps.CurrentPeek.Should().Be('c');

            ps.SkipToPeek();

            ps.Current.Should().Be('c');
        }
    }
}
