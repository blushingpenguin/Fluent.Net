using FluentAssertions;
using NUnit.Framework;
using System;
using System.IO;

namespace Fluent.Net.Test
{
    public class SerializerPaddingTest : FtlTestBase
    {
        void SerializeTwice(string input)
        {
            var serializer = new Serializer();
            var parser = new Parser();

            void Serialize()
            {
                using var sr = new StringReader(input);
                using var sw = new StringWriter();
                serializer.Serialize(sw, parser.Parse(sr));
                sw.ToString().Should().Be(input);
            }

            Serialize();

            // Run again to make sure the same instance of the serializer doesn't keep
            // state about how many entires is has already serialized.
            Serialize();
        }

        [Test]
        public void StandaloneCommentHasNotPaddingWhenFirst()
        {
            var input = Ftl(@"
              # Comment A

              foo = Foo

              # Comment B

              bar = Bar
            ");
            SerializeTwice(input);
        }

        [Test]
        public void GroupCommentHasNotPaddingWhenFirst()
        {
            var input = Ftl(@"
              ## Group A

              foo = Foo

              ## Group B

              bar = Bar
            ");
            SerializeTwice(input);
        }

        [Test]
        public void ResourceCommentHasNotPaddingWhenFirst()
        {
            var input = Ftl(@"
              ### Resource Comment A

              foo = Foo

              ### Resource Comment B

              bar = Bar
            ");
            SerializeTwice(input);
        }
    }
}
