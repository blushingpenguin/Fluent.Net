namespace Fluent.Net.Plural
{
    public class Range<T> where T : struct
    {
        public T Low { get; set; }
        public T? High { get; set; }

        public Range()
        {
        }

        public Range(T low, T? high = null)
        {
            Low = low;
            High = high;
        }
    }
}
