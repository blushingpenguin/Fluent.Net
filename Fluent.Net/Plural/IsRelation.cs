using System;

namespace Fluent.Net.Plural
{
    public class IsRelation : Relation
    {
        public int Value { get; set; }

        public override bool Match(string num)
        {
            return Decimal.Parse(num) == Value;
        }
    }
}
