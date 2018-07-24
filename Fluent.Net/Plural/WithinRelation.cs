namespace Fluent.Net.Plural
{
    public class WithinRelation : RangeRelation
    {
        public override bool Match(string num)
        {
            return Match(num, true);
        }
    }
}
