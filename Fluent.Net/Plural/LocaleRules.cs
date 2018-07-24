using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Fluent.Net.Plural
{
    public static class LocaleRules
    {
        static readonly IDictionary<string, Rules> s_localeRules;

        static LocaleRules()
        {
            // from: https://unicode.org/repos/cldr/trunk/common/supplemental/plurals.xml
            string[] resNames = typeof(LocaleRules).Assembly.GetManifestResourceNames();
            var asm = typeof(LocaleRules).Assembly;
            using (var stream = asm.GetManifestResourceStream(
                asm.GetName().Name + ".Plural.plurals.xml"))
            {
                var doc = new XmlDocument();
                doc.Load(stream);
                s_localeRules = ParseRules(doc);
            }
        }

        public static IDictionary<string, Rules> ParseRules(XmlDocument doc)
        {
            var result = new Dictionary<string, Rules>();
            foreach (XmlElement localeRules in doc.SelectNodes("supplementalData/plurals/pluralRules"))
            {
                var rules = new Rules() { CountRules = new List<Rule>() };
                foreach (XmlElement countRule in localeRules.SelectNodes("pluralRule"))
                {
                    var count = countRule.GetAttribute("count");
                    var ruleText = countRule.SelectSingleNode("text()").Value;
                    var rule = new Parser(ruleText ?? String.Empty).Parse();
                    rule.Name = count;
                    rules.CountRules.Add(rule);
                }

                string[] locales = localeRules.GetAttribute("locales").Split(
                    new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var locale in locales)
                {
                    result.Add(locale, rules);
                }
            }
            return result;
        }

        public static string Select(IEnumerable<string> locales, string num)
        {
            Rules rules = null;
            foreach (var locale in locales)
            {
                if (s_localeRules.TryGetValue(locale, out rules))
                {
                    break;
                }
            }
            if (rules != null)
            {
                return rules.Select(num);
            }
            return "other";
        }

        public static string Select(string locale, string num)
        {
            return Select(Enumerable.Repeat(locale, 1), num);
        }
    }
}
