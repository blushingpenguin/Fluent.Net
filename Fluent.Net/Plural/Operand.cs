namespace Fluent.Net.Plural
{
    public enum Operand
    {
        n, // absolute value of the source number(integer and decimals).
        i, // integer digits of n.
        v, // number of visible fraction digits in n, with trailing zeros.
        w, // number of visible fraction digits in n, without trailing zeros.
        f, // visible fractional digits in n, with trailing zeros.
        t  // visible fractional digits in n, without trailing zeros.
    }
}
