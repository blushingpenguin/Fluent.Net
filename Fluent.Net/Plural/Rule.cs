namespace Fluent.Net.Plural
{
    public class Rule
    {
        public string Name { get; set; }
        public Samples Samples { get; set; }
        public Condition Condition { get; set; }

        public bool Match(string num)
        {
            return Condition == null ? true : Condition.Match(num);
        }
    }
}
