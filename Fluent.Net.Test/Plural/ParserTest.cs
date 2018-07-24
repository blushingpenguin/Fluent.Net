using FluentAssertions;
using JsonDiffPatchDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fluent.Net.Plural;

using PParser = Fluent.Net.Plural.Parser;

namespace Fluent.Net.Test.Plural
{
    public class ParserTest
    {
        [Test]
        public void EmptyExpectOk()
        {
            var parser = new PParser("");
            Rule rule = parser.Parse();
            rule.Condition.Should().Be(null);
            rule.Name.Should().Be(null);
            rule.Samples.Should().Be(null);
        }

        [Test]
        public void DecimalSamplesOnlyExpectOk()
        {
            var parser = new PParser("@decimal 1.2,2.4,3.5~5.7,...");
            var expected = new Rule()
            {
                Condition = null,
                Name = null,
                Samples = new Samples()
                {
                    DecimalSamples = new List<Range<decimal>>()
                    {
                        new Range<decimal>(1.2M),
                        new Range<decimal>(2.4M),
                        new Range<decimal>(3.5M, 5.7M)
                    }
                }
            };
            Rule rule = parser.Parse();
            rule.Should().BeEquivalentTo(expected);
        }

        [Test]
        public void IntegerSamplesOnlyExpectOk()
        {
            var parser = new PParser("@integer 1,2,3~5,...");
            var expected = new Rule()
            {
                Condition = null,
                Name = null,
                Samples = new Samples()
                {
                    IntegerSamples = new List<Range<decimal>>()
                    {
                        new Range<decimal>(1),
                        new Range<decimal>(2),
                        new Range<decimal>(3, 5)
                    }
                }
            };
            Rule rule = parser.Parse();
            rule.Should().BeEquivalentTo(expected);
        }
    }
}
