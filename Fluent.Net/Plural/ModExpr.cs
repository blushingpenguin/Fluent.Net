namespace Fluent.Net.Plural
{
    public class ModExpr : Expr
    {
        public int Value { get; set; }

        public override decimal Evaluate(string num)
        {
            decimal v = base.Evaluate(num);
            return v % Value;
        }
    }
}
