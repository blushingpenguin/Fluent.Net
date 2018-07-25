using FluentAssertions;
using JsonDiffPatchDotNet;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
                message.Should().BeEquivalentTo(output,
                    options => options.RespectingRuntimeTypes());
            }
        }

        [Test]
        public void ErrorsAttachedAsAnnotations()
        {
            string ftl = "foo = ";
            var output = new Ast.Junk()
            {
                Content = "foo = ",
                Span = new Ast.Span(0, 6),
            };
            output.AddAnnotation(new Ast.Annotation()
            {
                Code = "E0005",
                Message = "Expected message \"foo\" to have a value or attributes",
                Args = new string[] { "foo" },
                Span = new Ast.Span(6, 6)
            });

            using (var sr = new StringReader(ftl))
            {
                var ps = new Parser(true);
                var message = ps.ParseEntry(sr);
                message.Should().BeEquivalentTo(output,
                    options => options.RespectingRuntimeTypes());
            }
        }

        [Test]
        public void TermWithAttributeTest()
        {
            string ftl = @"
                -Foo = foo
                    .bar = attr
            ";
            var output = new Ast.Term()
            {
                Attributes = new List<Ast.Attribute>()
                {
                    new Ast.Attribute()
                    {
                        Id = new Ast.Identifier()
                        {
                            Name = "bar",
                            Span = new Ast.Span(16, 19)
                        },
                        Value = new Ast.Pattern()
                        {
                            Elements = new List<Ast.SyntaxNode>()
                            {
                                new Ast.StringExpression() 
                                { 
                                    Span = new Ast.Span(22, 26),
                                    Value = "attr" 
                                },
                            },
                            Span = new Ast.Span(22, 26)
                        },
                        Span = new Ast.Span(15, 26)
                    }
                },
                Id = new Ast.Identifier()
                {
                    Name = "-Foo",
                    Span = new Ast.Span()
                    {
                        Start = 0,
                        End = 4
                    }
                },
                Value = new Ast.Pattern()
                {
                    Elements = new List<Ast.SyntaxNode>()
                    {
                        new Ast.StringExpression() 
                        { 
                            Span = new Ast.Span(7, 10),
                            Value = "foo"
                        }
                    },
                    Span = new Ast.Span(7, 10)
                },
                Span = new Ast.Span(0, 26)
            };
            using (var sr = new StringReader(Ftl(ftl)))
            {
                var ps = new Parser(true);
                var message = ps.ParseEntry(sr);
                message.Should().BeEquivalentTo(output,
                    options => options.RespectingRuntimeTypes());
            }
        }

        [Test]
        public void VariantTest()
        {
            string ftl = @"
                # Comment
                ## Group comment
                ### Resource comment
                foo = { 1 ->
                    *[one] One
                     [two] Two
                    }
            ";
            var output = new Ast.Resource()
            {
                Span = new Ast.Span(0, 97),
                Body = new List<Ast.Entry>()
                {
                    new Ast.Comment()
                    {
                        Span = new Ast.Span(0, 9),
                        Content = "Comment"
                    },
                    new Ast.GroupComment()
                    {
                        Span = new Ast.Span(10, 26),
                        Content = "Group comment"
                    },
                    new Ast.ResourceComment()
                    {
                        Span = new Ast.Span(27, 47),
                        Content = "Resource comment"
                    },
                    new Ast.Message()
                    {
                        Id = new Ast.Identifier()
                        {
                            Span = new Ast.Span(48, 51),
                            Name = "foo"
                        },
                        Span = new Ast.Span(48, 96),
                        Value = new Ast.Pattern()
                        {
                            Span = new Ast.Span(54, 96),
                            Elements = new List<Ast.SyntaxNode>()
                            {
                                new Ast.Placeable()
                                {
                                    Span = new Ast.Span(54, 96),
                                    Expression = new Ast.SelectExpression()
                                    {
                                        Span = new Ast.Span(55, 95),
                                        Expression = new Ast.NumberExpression()
                                        {
                                            Span = new Ast.Span(56, 57),
                                            Value = "1"
                                        },
                                        Variants = new List<Ast.Variant>()
                                        {
                                            new Ast.Variant()
                                            {
                                                Span = new Ast.Span(65, 75),
                                                IsDefault = true,
                                                Key = new Ast.VariantName()
                                                {
                                                    Span = new Ast.Span(67, 70),
                                                    Name = "one"
                                                },
                                                Value = new Ast.Pattern()
                                                {
                                                    Span = new Ast.Span(72, 75),
                                                    Elements = new List<Ast.SyntaxNode>()
                                                    {
                                                        new Ast.TextElement()
                                                        {
                                                            Span = new Ast.Span(72, 75),
                                                            Value = "One",
                                                        }
                                                    }
                                                }
                                            },
                                            new Ast.Variant()
                                            {
                                                Span = new Ast.Span(81, 90),
                                                IsDefault = false,
                                                Key = new Ast.VariantName()
                                                {
                                                    Span = new Ast.Span(82, 85),
                                                    Name = "two"
                                                },
                                                Value = new Ast.Pattern()
                                                {
                                                    Span = new Ast.Span(87, 90),
                                                    Elements = new List<Ast.SyntaxNode>()
                                                    {
                                                        new Ast.TextElement()
                                                        {
                                                            Span = new Ast.Span(87, 90),
                                                            Value = "Two",
                                                        }
                                                    }
                                                }
                                            },
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            using (var sr = new StringReader(Ftl(ftl)))
            {
                var ps = new Parser(true);
                var message = ps.Parse(sr);
                Console.WriteLine(AstToJson.ToJson(message));
                message.Should().BeEquivalentTo(output, options =>
                    options.RespectingRuntimeTypes());
            }
        }

        [Test]
        public void MiscellanousAstConstructors()
        {
            new Ast.AttributeExpression();
            new Ast.CallExpression();
            new Ast.ExternalArgument();
            new Ast.Function();
            new Ast.MessageReference();
            new Ast.NamedArgument();
            new Ast.NumberExpression();
            new Ast.VariantExpression();
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
                var resourceJson = AstToJson.ToJson(resource);
                bool resultsEqual = JToken.DeepEquals(resourceJson, testData.Expected);
                if (!resultsEqual)
                {
                    Console.WriteLine("parsed =");
                    Console.WriteLine(resourceJson);
                    Console.WriteLine("expected =");
                    Console.WriteLine(testData.Expected);
                    var jdp = new JsonDiffPatch();
                    var diff = jdp.Diff(resourceJson, testData.Expected);
                    Console.WriteLine("diff =");
                    Console.WriteLine(diff);
                }
                resultsEqual.Should().BeTrue();

                // doesn't seem to work -- just returns true
                // resourceJson.Should().BeEquivalentTo(testData.Expected,
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
