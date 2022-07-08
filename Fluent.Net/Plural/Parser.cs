using System;
using System.Collections.Generic;
using System.Text;

namespace Fluent.Net.Plural
{
    public class Parser
    {
        private int _index = 0;
        private readonly string _rule;

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
            NextAndSkipWhite();
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
                     digit > (Int32.MaxValue - 10 * (Int32.MaxValue / 10))))
                {
                    Error("Integer overflow", startPos);
                }
                num = num * 10 + digit;
            }
            SkipWhite();
            return num;
        }


        // expr          = operand (('mod' | '%') value)?
        Expr Expr()
        {
            var op = Operand_();
            if (Current == '%' ||
                (Current == 'm' && Peek(1) == 'o' && Peek(2) == 'd'))
            {
                NextAndSkipWhite(Current == '%' ? 1 : 3);
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
            List<AndCondition> conditions = null;
            for (; ; )
            {
                var andCondition = AndCondition();
                if (conditions == null)
                {
                    conditions = new List<AndCondition>();
                }
                conditions.Add(andCondition);
                if (Current != 'o' || Peek() != 'r')
                {
                    break;
                }
                NextAndSkipWhite(2);
            }
            return conditions == null ? null :
                new Condition() { Conditions = conditions };
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
            if (!Decimal.TryParse(valString.ToString(), out decimal val))
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
}
