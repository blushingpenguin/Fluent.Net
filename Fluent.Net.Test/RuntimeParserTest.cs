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
    public class RuntimeParserTest : ParserTestBase
    {
        public static StructureTestData ReadStructureFixture(string fixturesPath,
            string jsonPath, string json)
        {
            string testName = Path.GetFileNameWithoutExtension(jsonPath);
            string ftlPath = ResolvePath(fixturesPath + "/" + testName + ".ftl");
            return new StructureTestData()
            {
                TestName = testName,
                Ftl = File.ReadAllText(ftlPath),
                Expected = JObject.Parse(json)
            };
        }

        public static StructureTestData ReadStructureFixture(string jsonPath, string json)
        {
            return ReadStructureFixture("fixtures/full_structure", jsonPath, json);
        }

        public static IEnumerable<StructureTestData> ReadStructureFixtures()
        {
            return ForEachFile("fixtures/runtime_structure", "*.json",
                ReadStructureFixture);
        }

        [Test, TestCaseSource("ReadStructureFixtures")]
        public void StructureTest(StructureTestData testData)
        {
            using (var sr = new StringReader(testData.Ftl))
            {
                var resource = new RuntimeParser().GetResource(sr);

                var entryJson = RuntimeAstToJson.ToJson(resource.Entries);
                bool resultsEqual = JToken.DeepEquals(entryJson, testData.Expected);
                if (!resultsEqual)
                {
                    Console.WriteLine("parsed =");
                    Console.WriteLine(entryJson);
                    Console.WriteLine("expected =");
                    Console.WriteLine(testData.Expected);
                    var jdp = new JsonDiffPatch();
                    var diff = jdp.Diff(entryJson, testData.Expected);
                    Console.WriteLine("diff =");
                    Console.WriteLine(diff);
                }
                resultsEqual.Should().BeTrue();
            }
        }

        public static StructureTestData ReadBehaviourFixture(string jsonPath, string json)
        {
            return ReadStructureFixture("fixtures/full_behaviour", jsonPath, json);
        }

        public static IEnumerable<StructureTestData> ReadBehaviourFixtures()
        {
            return ForEachFile("fixtures/runtime_behaviour", "*.json",
                ReadBehaviourFixture);
        }

        [Test, TestCaseSource("ReadBehaviourFixtures")]
        public void BehaviourTest(StructureTestData testData)
        {
            using (var sr = new StringReader(testData.Ftl))
            {
                var resource = new RuntimeParser().GetResource(sr);
                var entryJson = RuntimeAstToJson.ToJson(resource.Entries);
                entryJson.Should().BeOfType<JObject>();
                var entries = (JObject)entryJson;

                // For some reason, this does a really shallow test -- just checks
                // keys in the objects rather than the contents.  Probably better
                // to just replace the expected outputs with the whole serialized
                // json.
                foreach (var expectedKeyValue in testData.Expected)
                {
                    entries.Should().ContainKey(expectedKeyValue.Key);
                    var entry = entries[expectedKeyValue.Key];

                    expectedKeyValue.Value.Should().NotBeNull();
                    expectedKeyValue.Value.Should().BeOfType<JObject>();
                    var expected = (JObject)expectedKeyValue.Value;

                    if (expected["value"].Type == JTokenType.Boolean &&
                        (bool)((JValue)expected["value"]).Value)
                    {
                        entry.Should().Match<JToken>(
                            x => x.Type == JTokenType.String ||
                            x.Type == JTokenType.Object && ((JObject)x).ContainsKey("val"));
                    }
                    else
                    {
                        entry.Should().Match<JToken>(
                            x => x.Type != JTokenType.String &&
                            !(x.Type == JTokenType.Object && ((JObject)x).ContainsKey("val")));
                    }

                    if (expected.ContainsKey("attributes"))
                    {
                        entry.Type.Should().Be(JTokenType.Object);
                        var entryObj = (JObject)entry;
                        entryObj.Should().ContainKey("attrs");

                        var expectedKeys = ((JObject)expected["attributes"])
                            .Properties().Select(x => x.Name);
                        var entryKeys = ((JObject)entryObj["attrs"])
                            .Properties().Select(x => x.Name);

                        expectedKeys.Should().BeEquivalentTo(entryKeys);
                    }
                    else
                    {
                        entry.Should().Match<JToken>(
                            x => x.Type == JTokenType.String ||
                                 (x.Type == JTokenType.Object &&
                                    !((JObject)x).ContainsKey("attrs")));
                    }
                }
            }
        }
    }
}
