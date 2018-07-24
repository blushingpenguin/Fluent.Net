namespace Fluent.Net.Plural
{
    public class InRelation : RangeRelation
    {
        public override bool Match(string num)
        {
            return Match(num, false);
        }
    }
}
