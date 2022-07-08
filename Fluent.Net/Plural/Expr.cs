using System;
using System.Diagnostics.CodeAnalysis;

namespace Fluent.Net.Plural
{
    public class Expr
    {
        public Operand Operand { get; set; }

        // absolute value of the source number(integer and decimals).
        [SuppressMessage("Style", "IDE1006:Naming rule violation", Justification = "Ported code")]
        decimal n(string num)
        {
            return Decimal.Parse(num);
        }

        // integer digits of n.
        [SuppressMessage("Style", "IDE1006:Naming rule violation", Justification = "Ported code")]
        decimal i(string num)
        {
            int pos = num.IndexOf('.');
            return Decimal.Parse(num.Substring(0, pos < 0 ? num.Length : pos));
        }

        // number of visible fraction digits in n, with trailing zeros.
        [SuppressMessage("Style", "IDE1006:Naming rule violation", Justification = "Ported code")]
        decimal v(string num)
        {
            int pos = num.IndexOf('.');
            return pos < 0 ? 0 : num.Length - pos - 1;
        }

        int LastNonZeroLength(string num)
        {
            int end = num.Length;
            for (; end > 0 && num[end - 1] == '0'; --end)
            {
            }
            return end;
        }

        // number of visible fraction digits in n, without trailing zeros.
        [SuppressMessage("Style", "IDE1006:Naming rule violation", Justification = "Ported code")]
        decimal w(string num)
        {
            int pos = num.IndexOf('.');
            if (pos < 0)
            {
                return 0;
            }
            int end = LastNonZeroLength(num);
            return end - pos - 1;
        }

        // visible fractional digits in n, with trailing zeros.
        [SuppressMessage("Style", "IDE1006:Naming rule violation", Justification = "Ported code")]
        decimal f(string num)
        {
            int pos = num.IndexOf('.');
            if (pos < 0)
            {
                return 0;
            }
            return Decimal.Parse(num.Substring(pos + 1));
        }

        // visible fractional digits in n, without trailing zeros.
        [SuppressMessage("Style", "IDE1006:Naming rule violation", Justification = "Ported code")]
        decimal t(string num)
        {
            int pos = num.IndexOf('.');
            if (pos < 0)
            {
                return 0;
            }
            int end = LastNonZeroLength(num);
            return Decimal.Parse(num.Substring(pos + 1, end - pos - 1));
        }

        public virtual decimal Evaluate(string num)
        {
            switch (Operand)
            {
                case Operand.n:
                    return n(num);
                case Operand.i:
                    return i(num);
                case Operand.v:
                    return v(num);
                case Operand.w:
                    return w(num);
                case Operand.f:
                    return f(num);
                case Operand.t:
                    return t(num);
                default:
                    throw new InvalidOperationException($"Unknown operand {Operand}");
            }
        }
    }
}
