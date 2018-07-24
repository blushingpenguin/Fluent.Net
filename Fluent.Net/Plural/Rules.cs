using System.Collections.Generic;
using System.Linq;

namespace Fluent.Net.Plural
{
    public class Rules
    {
        public ICollection<Rule> CountRules { get; set; }

        public string Select(string num)
        {
            var matchingRule = CountRules.Where(x => x.Match(num)).FirstOrDefault();
            return matchingRule?.Name ?? "other";
        }
    }
}
