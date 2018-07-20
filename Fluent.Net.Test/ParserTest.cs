using FluentAssertions;
using JsonDiffPatchDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Fluent.Net.Test
{
    public class ParserTest : ParserTestBase
    {
        [Test]
        public void SimpleMessage()
        {
            string ftl = "foo = Foo";

            var output = new Ast.Message()
            {
                Id = new Ast.Identifier()
                {
                    Name = "foo",
                    Span = new Ast.Span(0, 3)
                },
                Span = new Ast.Span(0, 9),
                Value = new Ast.Pattern()
                {
                    Elements = new Ast.SyntaxNode[] {
                        new Ast.TextElement()
                        {
                            Value = "Foo",
                            Span = new Ast.Span(6, 9)
                        }
                    },
                    Span = new Ast.Span(6, 9)
                },
            };

            using (var sr = new StringReader(ftl))
            {
                var ps = new Parser(true);
                var message = ps.ParseEntry(sr);
                message.Should().BeEquivalentTo(output);
            }
        }

        public static StructureTestData ReadStructureFixture(string jsonPath, string json)
        {
            string ftlPath = jsonPath.Substring(0,
                jsonPath.LastIndexOf('.') + 1) + "ftl";
            return new StructureTestData()
            {
                TestName = Path.GetFileNameWithoutExtension(jsonPath),
                Ftl = File.ReadAllText(ftlPath),
                Expected = JObject.Parse(json)
            };
        }

        public static IEnumerable<StructureTestData> ReadStructureFixtures()
        {
            return ForEachFile("fixtures/full_structure", "*.json",
                ReadStructureFixture);
        }

        [Test, TestCaseSource("ReadStructureFixtures")]
        public void StructureTest(StructureTestData testData)
        {
            using (var sr = new StringReader(testData.Ftl))
            {
                var resource = new Parser().Parse(sr);

                bool resultsEqual = JToken.DeepEquals(resource.ToJson(), testData.Expected);
                if (!resultsEqual)
                {
                    Console.WriteLine("parsed =");
                    Console.WriteLine(resource.ToJson());
                    Console.WriteLine("expected =");
                    Console.WriteLine(testData.Expected);
                    var jdp = new JsonDiffPatch();
                    var diff = jdp.Diff(resource.ToJson(), testData.Expected);
                    Console.WriteLine("diff =");
                    Console.WriteLine(diff);
                }
                resultsEqual.Should().BeTrue();

                // doesn't seem to work -- just returns true
                // resource.ToJson().Should().BeEquivalentTo(testData.Expected,
                //    options => options.AllowingInfiniteRecursion().RespectingRuntimeTypes());
            }
        }

        public static IEnumerable<BehaviourTestData> ReadBehaviourFixtures()
        {
            return ForEachFile("fixtures/full_behaviour", "*.ftl",
                ParseBehaviourFixture);
        }

        [Test, TestCaseSource("ReadBehaviourFixtures")]
        public void BehaviourTest(BehaviourTestData testData)
        {
            using (var sr = new StringReader(testData.Source))
            {
                var parser = new Parser();
                var ast = parser.Parse(sr);
                //var wotst
                var actual = String.Join("\n",
                    ast.Body.SelectMany(x => x.Annotations.Select(y => SerializeAnnotation(y)))) + "\n";
                actual.Should().Be(testData.Expected);
            }
        }
    }
}
