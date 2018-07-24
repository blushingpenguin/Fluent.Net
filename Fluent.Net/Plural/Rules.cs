using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Fluent.Net.Plural
{
    /*
    rules         = rule (';' rule)*
    rule          = keyword ':' condition samples
                  | 'other' ':' samples
    keyword       = [a-z]+
    condition     = and_condition ('or' and_condition)*
    samples       = ('@integer' sampleList)?
                    ('@decimal' sampleList)?                
    and_condition = relation ('and' relation)*
    relation      = is_relation | in_relation | within_relation 
    is_relation   = expr 'is' ('not')? value
    in_relation   = expr (('not')? 'in' | '=' | '!=') range_list
    within_relation = expr ('not')? 'within' range_list
    expr          = operand (('mod' | '%') value)?
    operand       = 'n' | 'i' | 'f' | 't' | 'v' | 'w'
    range_list    = (range | value) (',' range_list)*
    range         = value'..'value
    sampleList    = sampleRange (',' sampleRange)* (',' ('…'|'...'))?
    sampleRange   = decimalValue ('~' decimalValue)?
    value         = digit+
    decimalValue  = value ('.' value)?
    digit         = 0|1|2|3|4|5|6|7|8|9
    */
    public class ParseException : Exception
    {
        public ParseException(string msg) :
            base(msg)
        {
        }
    }

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

    public class Samples
    {
        public IList<Range<decimal>> IntegerSamples { get; set; }
        public IList<Range<decimal>> DecimalSamples { get; set; }
    }

    public abstract class Relation
    {
        public Expr Expr { get; set; }
        public bool Not { get; set; }

        public abstract bool Match(string num);
    }

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

    public class InRelation : RangeRelation
    {
        public override bool Match(string num)
        {
            return Match(num, false);
        }
    }

    public class WithinRelation : RangeRelation
    {
        public override bool Match(string num)
        {
            return Match(num, true);
        }
    }

    public class IsRelation : Relation
    {
        public int Value { get; set; }

        public override bool Match(string num)
        {
            return Decimal.Parse(num) == Value;
        }
    }

    public class AndCondition
    {
        public IList<Relation> Relations { get; set; }

        public bool Match(string num)
        {
            return Relations.All(x => x.Match(num));
        }
    }

    public class Condition
    {
        public IList<AndCondition> Conditions { get; set; }

        public bool Match(string num)
        {
            return Conditions.Any(x => x.Match(num));
        }
    }

    public class Rule
    {
        public string Name { get; set; }
        public Samples Samples { get; set; }
        public Condition Condition { get; set; }

        public bool Match(string num)
        {
            return Condition.Match(num);
        }
    }

    public enum Operand
    {
        n, // absolute value of the source number(integer and decimals).
        i, // integer digits of n.
        v, // number of visible fraction digits in n, with trailing zeros.
        w, // number of visible fraction digits in n, without trailing zeros.
        f, // visible fractional digits in n, with trailing zeros.
        t  // visible fractional digits in n, without trailing zeros.
    }

    public class Expr
    {
        public Operand Operand { get; set; }

        // absolute value of the source number(integer and decimals).
        decimal n(string num)
        {
            return Decimal.Parse(num);
        }

        // integer digits of n.
        decimal i(string num)
        {
            int digits = 0;
            for (; digits < num.Length &&
                   num[digits] >= '0' && num[digits] <= '9'; ++digits)
            {
            }
            return digits;
        }

        // number of visible fraction digits in n, with trailing zeros.
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

    public class ModExpr : Expr
    {
        public int Value;
    }

    public class Parser
    {
        int _index = 0;
        string _rule;

        public Parser(string rule)
        {
            _rule = rule;
        }

        int Current
        {
            get { return _index < _rule.Length ? _rule[_index] : -1; }
        }

        string CurrentChar
        {
            get { return Current == -1 ? "Eof" : ((char)Current).ToString(); }
        }

        int Peek(int n = 1)
        {
            return _index + n < _rule.Length ? _rule[_index + n] : -1;
        }

        void Next(int n = 1)
        {
            if (_index + n > _rule.Length)
            {
                throw new InvalidOperationException("Internal error: attempted to seek past end");
            }
            _index += n;
        }

        bool IsWhite(int c)
        {
            return c == '\u200e' || c == '\u200f' ||
                   (c >= '\u0009' && c <= '\u000d') ||
                   c == '\u0085' || c == '\u2028' ||
                   c == '\u2029' || c == '\u0020';
        }

        void SkipWhite()
        {
            while (IsWhite(Current))
            {
                Next();
            }
        }

        void NextAndSkipWhite(int n = 1)
        {
            Next(n);
            SkipWhite();
        }

        ParseException Error(string msg, int pos = -1)
        {
            throw new ParseException($"{msg} at position " +
                $"{(pos == -1 ? _index : pos) + 1}");
        }

        // operand       = 'n' | 'i' | 'f' | 't' | 'v' | 'w'
        Operand Operand_()
        {
            Operand op;
            switch (Current)
            {
                case 'n':
                    op = Operand.n;
                    break;
                case 'i':
                    op = Operand.i;
                    break;
                case 'f':
                    op = Operand.f;
                    break;
                case 't':
                    op = Operand.t;
                    break;
                case 'v':
                    op = Operand.v;
                    break;
                case 'w':
                    op = Operand.w;
                    break;
                default:
                    throw Error($"Unknown operand '{CurrentChar}'");
            }
            Next();
            SkipWhite();
            return op;
        }


        // value         = digit+
        // digit         = 0|1|2|3|4|5|6|7|8|9
        int Value()
        {
            int startPos = _index;

            if (Current < '0' || Current > '9')
            {
                Error($"Expected digit but found '{CurrentChar}'");
            }
            int num = Current - '0';
            for (Next(); Current >= '0' && Current <= '9'; Next())
            {
                int digit = Current - '0';
                // 2,147,483,647
                if (num > Int32.MaxValue / 10 || // 214748364
                    (num == Int32.MaxValue / 10 &&
                     digit > (Int32.MaxValue - Int32.MaxValue / 10)))
                {
                    Error("Integer overflow", startPos);
                }
                num = num * 10 + digit;
            }
            return num;
        }


        // expr          = operand (('mod' | '%') value)?
        Expr Expr()
        {
            var op = Operand_();
            if (Peek() == '%' ||
                (Peek() == 'm' && Peek(2) == 'o' && Peek(3) == 'd'))
            {
                NextAndSkipWhite(Peek() == '%' ? 1 : 3);
                var value = Value();
                return new ModExpr() { Operand = op, Value = value };
            }
            return new Expr() { Operand = op };
        }

        bool OptionalNot()
        {
            if (Current == 'n' && Peek() == 'o' && Peek(2) == 't')
            {
                NextAndSkipWhite(3);
                return true;
            }
            return false;
        }

        // is_relation   = expr 'is' ('not')? value
        Relation IsRelation(Expr expr)
        {
            bool not = OptionalNot();
            int value = Value();
            return new IsRelation()
            {
                Expr = expr,
                Not = not,
                Value = value
            };
        }

        // range_list    = (range | value) (',' range_list)*
        // range         = value'..'value
        IList<Range<int>> RangeList()
        {
            IList<Range<int>> ranges = new List<Range<int>>();
            for (; ; )
            {
                int low = Value();
                int? high = null;
                SkipWhite();
                if (Current == '.')
                {
                    Next();
                    if (Current != '.')
                    {
                        Error("Expected '..'", _index - 1);
                    }
                    NextAndSkipWhite();
                    high = Value();
                }
                ranges.Add(new Range<int>(low, high));
                if (Current != ',')
                {
                    break;
                }
                NextAndSkipWhite();
            }
            return ranges;
        }

        // in_relation   = expr (('not')? 'in' | '=' | '!=') range_list
        Relation InRelation(bool not, Expr expr)
        {
            if (Current == 'i' && Peek() == 'n')
            {
                NextAndSkipWhite(2);
            }
            else if (Current == '=')
            {
                NextAndSkipWhite();
            }
            else if (Current == '!' && Peek() == '=')
            {
                NextAndSkipWhite(2);
                not = !not;
            }
            else
            {
                Error("Expected 'within', 'in', '=' or '!='");
            }
            var ranges = RangeList();
            return new InRelation()
            {
                Expr = expr,
                Not = not,
                Ranges = ranges
            };
        }

        // within_relation = expr ('not')? 'within' range_list
        Relation WithinRelation(bool not, Expr expr)
        {
            var ranges = RangeList();
            return new WithinRelation()
            {
                Expr = expr,
                Not = not,
                Ranges = ranges
            };
        }

        // relation      = is_relation | in_relation | within_relation
        Relation Relation()
        {
            var expr = Expr();
            if (Current == 'i' && Peek() == 's')
            {
                Next(2);
                SkipWhite();
                return IsRelation(expr);
            }
            else
            {
                bool not = OptionalNot();
                if (Current == 'w' && Peek() == 'i' && Peek(2) == 't' &&
                    Peek(3) == 'h' && Peek(4) == 'i' && Peek(5) == 'n')
                {
                    NextAndSkipWhite(6);
                    return WithinRelation(not, expr);
                }
                else
                {
                    return InRelation(not, expr);
                }
            }
        }

        // and_condition = relation ('and' relation)*
        AndCondition AndCondition()
        {
            var andCondition = new AndCondition()
            {
                Relations = new List<Relation>()
            };
            for (; ; )
            {
                var relation = Relation();
                andCondition.Relations.Add(relation);
                SkipWhite();
                if (Current != 'a' || Peek() != 'n' || Peek(2) != 'd')
                {
                    break;
                }
                NextAndSkipWhite(3);
            }
            return andCondition;
        }

        // condition     = and_condition ('or' and_condition)*
        Condition Condition()
        {
            var condition = new Condition()
            {
                Conditions = new List<AndCondition>()
            };
            for (; ; )
            {
                var andCondition = AndCondition();
                condition.Conditions.Add(andCondition);
                if (Current != 'o' || Peek() != 'r')
                {
                    break;
                }
                NextAndSkipWhite(2);
            }
            return condition;
        }

        // decimalValue  = value ('.' value)?
        decimal DecimalValue()
        {
            if (Current < '0' || Current > '9')
            {
                Error($"Expected digit but found '{CurrentChar}'");
            }
            var buf = new StringBuilder();
            bool seenDot = false;
            for (; ; )
            {
                buf.Append((char)Current);
                Next();
                if ((Current < '0' || Current > '9') &&
                    !(Current == '.' && !seenDot))
                {
                    break;
                }
                seenDot = seenDot || Current == '.';
            }
            var valString = buf.ToString();
            decimal val;
            if (!Decimal.TryParse(valString.ToString(), out val))
            {
                Error($"Invalid decimal value {valString}");
            }
            return val;
        }

        // sampleRange   = decimalValue ('~' decimalValue)?
        Range<decimal> SampleRange()
        {
            var low = DecimalValue();
            SkipWhite();
            if (Current != '~')
            {
                return new Range<decimal>(low);
            }
            NextAndSkipWhite();
            var high = DecimalValue();
            return new Range<decimal>(low, high);
        }

        // sampleList    = sampleRange (',' sampleRange)* (',' ('…'|'...'))?
        IList<Range<decimal>> SampleList()
        {
            var ranges = new List<Range<decimal>>();
            for (; ; )
            {
                ranges.Add(SampleRange());
                SkipWhite();
                if (Current != ',')
                {
                    break;
                }
                NextAndSkipWhite();
                if (Current == '…')
                {
                    Next();
                    break;
                }
                if (Current == '.' && Peek() == '.' && Peek(2) == '.')
                {
                    Next(3);
                    break;
                }
            }
            return ranges;
        }

        // samples       = ('@integer' sampleList)?
        //                 ('@decimal' sampleList)?
        Samples Samples()
        {
            IList<Range<decimal>> integerSamples = null, decimalSamples = null;
            var samples = new Samples();
            if (Current == '@' && Peek() == 'i' && Peek(2) == 'n' &&
                Peek(3) == 't' && Peek(4) == 'e' && Peek(5) == 'g' &&
                Peek(6) == 'e' && Peek(7) == 'r')
            {
                // @integer
                NextAndSkipWhite(8);
                integerSamples = SampleList();
                SkipWhite();
            }
            if (Current == '@' && Peek() == 'd' && Peek(2) == 'e' &&
                Peek(3) == 'c' && Peek(4) == 'i' && Peek(5) == 'm' &&
                Peek(6) == 'a' && Peek(7) == 'l')
            {
                // @decimal
                NextAndSkipWhite(8);
                decimalSamples = SampleList();
            }
            if (integerSamples != null || decimalSamples != null)
            {
                return new Samples()
                {
                    IntegerSamples = integerSamples,
                    DecimalSamples = decimalSamples
                };
            }
            return null;
        }

        public Rule Parse()
        {
            var rule = new Rule();

            SkipWhite();
            // an empty condition is valid (contrary to the grammar, 
            // but the text / examples allow it)
            if (Current != -1 && Current != '@')
            {
                rule.Condition = Condition();
            }
            rule.Samples = Samples();
            SkipWhite();
            if (Current != -1)
            {
                Error($"Unexpected trailing character {CurrentChar}");
            }

            return rule;
        }
    }

    public class Rules
    {
        public ICollection<Rule> CountRules { get; set; }

        public string Select(string num)
        {
            var matchingRule = CountRules.Where(x => x.Match(num)).FirstOrDefault();
            return matchingRule?.Name ?? "other";
        }
    }

    public class LocaleRules
    {
        static readonly IDictionary<string, Rules> s_localeRules;

        static LocaleRules()
        {
            // from: https://unicode.org/repos/cldr/trunk/common/supplemental/plurals.xml
            using (var stream = typeof(LocaleRules).Assembly
                .GetManifestResourceStream("Fluent.Net.Plural.plurals.xml"))
            {
                var doc = new XmlDocument();
                doc.Load(stream);
                s_localeRules = ParseRules(doc);
            }
        }

        static IDictionary<string, Rules> ParseRules(XmlDocument doc)
        {
            var result = new Dictionary<string, Rules>();
            foreach (XmlElement localeRules in doc.SelectNodes("supplementalData/plurals/pluralRules"))
            {
                var rules = new Rules() { CountRules = new List<Rule>() };
                foreach (XmlElement countRule in localeRules.SelectNodes("pluralRule"))
                {
                    var count = countRule.GetAttribute("count");
                    var rule = new Parser(countRule.Value).Parse();
                    rule.Name = count;
                    rules.CountRules.Add(rule);
                }

                string[] locales = localeRules.GetAttribute("locales").Split(
                    new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var locale in locales)
                {
                    result.Add(locale, rules);
                }
            }
            return result;
        }

        static string Select(string locale, string num)
        {
            Rules rules;
            if (s_localeRules.TryGetValue(locale, out rules))
            {
                return rules.Select(num);
            }
            return "other";
        }
    }
}
