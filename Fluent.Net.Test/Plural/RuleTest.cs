using Fluent.Net.Plural;
using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;

namespace Fluent.Net.Test.Plural
{
    public class RuleTest
    {
        [Test]
        public void TestNExpr()
        {
            var expr = new Expr() { Operand = Operand.n };
            expr.Evaluate("2134.432000").Should().Be(2134.432M);
        }

        [Test]
        public void TestIExpr()
        {
            var expr = new Expr() { Operand = Operand.i };
            expr.Evaluate("2134.432000").Should().Be(2134M);
        }

        [Test]
        public void TestVExpr()
        {
            var expr = new Expr() { Operand = Operand.v };
            expr.Evaluate("2134.432000").Should().Be(6M);
        }

        [Test]
        public void TestWExpr()
        {
            var expr = new Expr() { Operand = Operand.w };
            expr.Evaluate("2134.432000").Should().Be(3M);
        }

        [Test]
        public void TestFExpr()
        {
            var expr = new Expr() { Operand = Operand.f };
            expr.Evaluate("2134.432000").Should().Be(432000M);
        }

        [Test]
        public void TestTExpr()
        {
            var expr = new Expr() { Operand = Operand.t };
            expr.Evaluate("2134.432000").Should().Be(432M);
        }

        [Test]
        public void TestModExpr()
        {
            var expr = new ModExpr() { Operand = Operand.n, Value = 6 };
            expr.Evaluate("99").Should().Be(3M);
        }

        [Test]
        public void TestIsRelation()
        {
            var expr = new IsRelation()
            {
                Expr = new Expr() { Operand = Operand.n },
                Value = 456
            };
            expr.Match("456").Should().BeTrue();
            expr.Match("123").Should().BeFalse();
        }

        [Test]
        public void TestWithinRelation()
        {
            var expr = new WithinRelation()
            {
                Expr = new Expr() { Operand = Operand.n },
                Ranges = new List<Range<int>>()
                {
                    new Range<int>(50, 60)
                }
            };
            expr.Match("45").Should().BeFalse();
            expr.Match("49").Should().BeFalse();
            expr.Match("50").Should().BeTrue();
            expr.Match("50.99").Should().BeTrue();
            expr.Match("55.123").Should().BeTrue();
            expr.Match("60").Should().BeTrue();
            expr.Match("61").Should().BeFalse();

            // invert the logic
            expr.Not = true;
            expr.Match("45").Should().BeTrue();
            expr.Match("49").Should().BeTrue();
            expr.Match("50").Should().BeFalse();
            expr.Match("50.99").Should().BeFalse();
            expr.Match("55.123").Should().BeFalse();
            expr.Match("60").Should().BeFalse();
            expr.Match("61").Should().BeTrue();
        }

        [Test]
        public void TestInRelation()
        {
            var expr = new InRelation()
            {
                Expr = new Expr() { Operand = Operand.n },
                Ranges = new List<Range<int>>()
                {
                    new Range<int>(2, 5),
                    new Range<int>(10, 14)
                }
            };
            expr.Match("1").Should().BeFalse();
            expr.Match("2").Should().BeTrue();
            expr.Match("3").Should().BeTrue();
            expr.Match("4").Should().BeTrue();
            expr.Match("5").Should().BeTrue();
            expr.Match("5.4").Should().BeFalse();
            expr.Match("6").Should().BeFalse();

            expr.Match("9").Should().BeFalse();
            expr.Match("10").Should().BeTrue();
            expr.Match("11").Should().BeTrue();
            expr.Match("12").Should().BeTrue();
            expr.Match("13").Should().BeTrue();
            expr.Match("14").Should().BeTrue();
            expr.Match("15").Should().BeFalse();

            expr.Not = true;

            expr.Match("1").Should().BeTrue();
            expr.Match("2").Should().BeFalse();
            expr.Match("3").Should().BeFalse();
            expr.Match("4").Should().BeFalse();
            expr.Match("5").Should().BeFalse();
            expr.Match("5.4").Should().BeTrue();
            expr.Match("6").Should().BeTrue();

            expr.Match("9").Should().BeTrue();
            expr.Match("10").Should().BeFalse();
            expr.Match("11").Should().BeFalse();
            expr.Match("12").Should().BeFalse();
            expr.Match("13").Should().BeFalse();
            expr.Match("14").Should().BeFalse();
            expr.Match("15").Should().BeTrue();
        }

        [Test]
        public void TestAndCondition()
        {
            var condition = new AndCondition()
            {
                Relations = new List<Relation>()
                {
                    new IsRelation()
                    {
                        Expr = new Expr() { Operand = Operand.n },
                        Value = 4
                    },
                    new InRelation()
                    {
                        Expr = new Expr() { Operand = Operand.n },
                        Ranges = new List<Range<int>>()
                        {
                            new Range<int>(3, 7)
                        }
                    }
                }
            };
            condition.Match("4").Should().BeTrue();
            condition.Match("5").Should().BeFalse();
        }

        [Test]
        public void TestOrCondition()
        {
            var condition = new Condition()
            {
                Conditions = new List<AndCondition>()
                {
                    new AndCondition()
                    {
                        Relations = new List<Relation>()
                        {
                            new IsRelation()
                            {
                                Expr = new Expr() { Operand = Operand.n },
                                Value = 100
                            }
                        }
                    },
                    new AndCondition()
                    {
                        Relations = new List<Relation>()
                        {
                            new IsRelation()
                            {
                                Expr = new Expr() { Operand = Operand.n },
                                Value = 105
                            }
                        }
                    }
                }
            };
            condition.Match("99").Should().BeFalse();
            condition.Match("100").Should().BeTrue();
            condition.Match("105").Should().BeTrue();
        }

        [Test]
        public void TestNoConditionRule()
        {
            var rule = new Rule()
            {
                Name = "other",
                Condition = null
            };
            rule.Match("123").Should().BeTrue();
            rule.Match("999.9999").Should().BeTrue();
            rule.Match("junk").Should().BeTrue();
        }

        [Test]
        public void TestRuleWithCondition()
        {
            var rule = new Rule()
            {
                Name = "one",
                Condition = new Condition()
                {
                    Conditions = new List<AndCondition>()
                    {
                        new AndCondition()
                        {
                            Relations = new List<Relation>()
                            {
                                new IsRelation()
                                {
                                    Expr = new Expr() { Operand = Operand.v },
                                    Value = 1
                                }
                            }
                        }
                    }
                }
            };
            rule.Match("1").Should().BeTrue();
            rule.Match("2").Should().BeFalse();
        }
    }
}
