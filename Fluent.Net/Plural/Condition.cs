using System.Collections.Generic;
using System.Linq;

namespace Fluent.Net.Plural
{
    public class Condition
    {
        public IList<AndCondition> Conditions { get; set; }

        public bool Match(string num)
        {
            return Conditions.Any(x => x.Match(num));
        }
    }
}
