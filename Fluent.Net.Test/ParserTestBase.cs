using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Fluent.Net.Test
{
    public abstract class ParserTestBase : FtlTestBase
    {
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

        static readonly Regex s_reDirective = new Regex("^# ~(.*)[\n$]",
            RegexOptions.Compiled | RegexOptions.Multiline);

        class FtlWithDirectives
        {
            public IEnumerable<string> Directives { get; set; }
            public string Ftl { get; set; }
        }

        public static BehaviourTestData ParseBehaviourFixture(string ftlPath, string ftl)
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

        static string GetCodeName(string code)
        {
            return code[0] switch
            {
                'E' => $"ERROR {code}",
                _ => throw new InvalidOperationException($"Unknown Annotation code {code}"),
            };
        }

        public static string SerializeAnnotation(Ast.Annotation annotation)
        {
            var parts = new List<string>
            {
                GetCodeName(annotation.Code)
            };

            int start = annotation.Span.Start.Offset,
                end = annotation.Span.End.Offset;
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
            return Path.GetFullPath("../../../" + path);
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
    }
}
