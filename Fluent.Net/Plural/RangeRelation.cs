using System;
using System.Collections.Generic;

namespace Fluent.Net.Plural
{
    public abstract class RangeRelation : Relation
    {
        public IList<Range<int>> Ranges { get; set; }

        protected bool Match(string num, bool allowFractions)
        {
            decimal exprValue = Expr.Evaluate(num);
            bool canRangeMatch = allowFractions ||
                exprValue == Math.Floor(exprValue);
            foreach (var range in Ranges)
            {
                if ((!range.High.HasValue && exprValue == range.Low) ||
                    (canRangeMatch && range.High.HasValue &&
                     exprValue >= range.Low && exprValue <= range.High))
                {
                    return !Not;
                }
            }
            return Not;
        }
    }
}
