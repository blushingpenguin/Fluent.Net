using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Fluent.Net.Test
{
    static class Util
    {
        private readonly static Regex s_blankRe = new Regex("^\\s*$");
        private readonly static Regex s_blankCountRe = new Regex("^\\s*");

        public static string Ftl(string input)
        {
            input = input.Replace("\r\n", "\n");
            string[] allLines = input.Split('\n');
            string[] lines = new string[allLines.Length - 2];
            Array.Copy(allLines, 1, lines, 0, lines.Length);
            int common = lines.Where(x => !s_blankRe.IsMatch(x))
                .Select(x => s_blankCountRe.Match(x).Value.Length).Min();
            var indent = new Regex($"^\\s{{{common}}}");
            var dedented = lines.Select(line => indent.Replace(line, ""));
            return String.Join('\n', dedented) + "\n";
        }
    }
}
