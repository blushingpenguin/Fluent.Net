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
    public class ParserTest
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

        public class StructureTestData
        {
            public string TestName { get; set; }
            public string Ftl { get; set; }
            public JObject Expected { get; set; }

            public override string ToString()
            {
                return TestName;
            }
        }

        public static string ResolvePath(string path)
        {
            return "../../../" + path;
        }

        public static IEnumerable<T> ForEachFile<T>(string path, string filter,
            Func<string, string, T> fn)
        {
            foreach (string filePath in Directory.GetFiles(
                ResolvePath(path), filter))
            {
                yield return fn(filePath, File.ReadAllText(filePath));
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
            return ForEachFile("fixtures_structure", "*.json",
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

        public class BehaviourTestData
        {
            public string TestName { get; set; }
            public string Expected { get; set; }
            public string Source { get; set; }

            public override string ToString()
            {
                return TestName;
            }
        }

        static readonly Regex s_reDirective = new Regex("^//~ (.*)[\n$]", 
            RegexOptions.Compiled | RegexOptions.Multiline);

        class FtlWithDirectives
        {
            public IEnumerable<string> Directives { get; set; }
            public string Ftl { get; set; }
        }

        static FtlWithDirectives ProcessFtlWithDirectives(string ftl)
        {
            return new FtlWithDirectives()
            {
                Directives = s_reDirective.Matches(ftl).Select(x => x.Captures[0].Value),
                Ftl = s_reDirective.Replace(ftl, "")
            };
        }

        static BehaviourTestData ParseBehaviourFixture(string ftlPath, string ftl)
        {
            var expected =
                String.Join("\n", s_reDirective.Matches(ftl)
                    .Select(x => x.Groups[1].Value)) + "\n";
            var source = s_reDirective.Replace(ftl, "");
            return new BehaviourTestData()
            {
                TestName = Path.GetFileNameWithoutExtension(ftlPath),
                Expected = expected,
                Source = source
            };
        }

        public static IEnumerable<BehaviourTestData> ReadBehaviourFixtures()
        {
            return ForEachFile("fixtures_behaviour", "*.ftl",
                ParseBehaviourFixture);
        }
        /*


export function serializeAnnotation(annot) {
}

function toDirectives(annots, cur) {
  return annots.concat(cur.annotations.map(serializeAnnotation));
}
        return readfile(filepath).then(file => {
          const { directives, source } = preprocess(file);
          const expected = directives.join('\n') + '\n';
          const ast = parse(source);
          const actual = ast.body.reduce(toDirectives, []).join('\n') + '\n';
          assert.deepEqual(
            actual, expected, 'Annotations mismatch'
          );
        });

*/
        static string GetCodeName(string code)
        {
            switch (code[0])
            {
                case 'E':
                    return $"ERROR {code}";
                case 'W':
                    return $"WARNING ${code}";
                case 'H':
                    return $"HINT ${code}";
                default:
                    throw new InvalidOperationException($"Unknown Annotation code {code}");
            }
        }

        static string SerializeAnnotation(Ast.Annotation annotation)
        {
            var parts = new List<string>();
            parts.Add(GetCodeName(annotation.Code));

            int start = annotation.Span.Start,
                end = annotation.Span.End;
            if (start == end)
            {
                parts.Add($"pos {start}");
            }
            else
            {
                parts.Add($"start {start}");
                parts.Add($"start {end}");
            }

            var args = annotation.Args;
            if (args != null && args.Length > 0)
            {
                var prettyArgs = String.Join(" ", args.Select(arg => $"\"{arg}\""));
                parts.Add($"args {prettyArgs}");
            }

            return String.Join(", ", parts);
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
        /*
        suite('Parse entry', function() {
          setup(function() {
            this.parser = new FluentParser();
          });


        suite('Serialize entry', function() {
          setup(function() {
            this.serializer = new FluentSerializer();
          });

          test('simple message', function() {
            const input = {
              "comment": null,
              "span": {
                "start": 0,
                "end": 9,
                "type": "Span"
              },
              "value": {
                "elements": [
                  {
                    "type": "TextElement",
                    "value": "Foo"
                  }
                ],
                "type": "Pattern",
                "span": {
                  "start": 6,
                  "end": 9,
                  "type": "Span"
                }
              },
              "annotations": [],
              "attributes": [],
              "type": "Message",
              "id": {
                "type": "Identifier",
                "name": "foo",
                "span": {
                  "start": 0,
                  "end": 3,
                  "type": "Span"
                }
              }
            };
            const output = ftl`
              foo = Foo
            `;

            const message = this.serializer.serializeEntry(input)
            assert.deepEqual(message, output)
          });
        });
        */
    }
}
