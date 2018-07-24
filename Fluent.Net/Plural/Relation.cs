namespace Fluent.Net.Plural
{
    public abstract class Relation
    {
        public Expr Expr { get; set; }
        public bool Not { get; set; }

        public abstract bool Match(string num);
    }
}
