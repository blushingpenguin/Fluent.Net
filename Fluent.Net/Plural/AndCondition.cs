using System.Collections.Generic;
using System.Linq;

namespace Fluent.Net.Plural
{
    public class AndCondition
    {
        public IList<Relation> Relations { get; set; }

        public bool Match(string num)
        {
            return Relations.All(x => x.Match(num));
        }
    }
}
