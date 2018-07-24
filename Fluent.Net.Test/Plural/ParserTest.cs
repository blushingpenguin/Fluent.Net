using Fluent.Net.Plural;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;

using PParseException = Fluent.Net.Plural.ParseException;
using PParser = Fluent.Net.Plural.Parser;

namespace Fluent.Net.Test.Plural
{
    public class ParserTest
    {
        [Test]
        public void EmptyExpectOk()
        {
            var parser = new PParser("");
            Rule rule = parser.Parse();
            rule.Condition.Should().Be(null);
            rule.Name.Should().Be(null);
            rule.Samples.Should().Be(null);
        }

        [Test]
        public void DecimalSamplesOnlyExpectOk()
        {
            var parser = new PParser("@decimal 1.2,2.4,3.5~5.7,...");
            var expected = new Rule()
            {
                Condition = null,
                Name = null,
                Samples = new Samples()
                {
                    DecimalSamples = new List<Range<decimal>>()
                    {
                        new Range<decimal>(1.2M),
                        new Range<decimal>(2.4M),
                        new Range<decimal>(3.5M, 5.7M)
                    }
                }
            };
            Rule rule = parser.Parse();
            rule.Should().BeEquivalentTo(expected);
        }

        [Test]
        public void IntegerSamplesOnlyExpectOk()
        {
            var parser = new PParser("@integer 1,2,3~5,...");
            var expected = new Rule()
            {
                Condition = null,
                Name = null,
                Samples = new Samples()
                {
                    IntegerSamples = new List<Range<decimal>>()
                    {
                        new Range<decimal>(1),
                        new Range<decimal>(2),
                        new Range<decimal>(3, 5)
                    }
                }
            };
            Rule rule = parser.Parse();
            rule.Should().BeEquivalentTo(expected);
        }

        [Test]
        public void SamplesWithInvalidNumbersExpectParseException()
        {
            Action a = () => new PParser("@integer `string`,...").Parse();
            a.Should().Throw<PParseException>();
        }

        [Test]
        public void RuleWithFractions()
        {
            var parser = new PParser("v = 0 and i % 10 = 1 and i % 100 != 11 or f % 10 = 1 and f % 100 != 11 @integer 1, 21, 31, 41, 51, 61, 71, 81, 101, 1001, … @decimal 0.1, 1.1, 2.1, 3.1, 4.1, 5.1, 6.1, 7.1, 10.1, 100.1, 1000.1, …");
            var expected = new Rule()
            {
                Condition = new Condition()
                {
                    Conditions = new List<AndCondition>()
                    {
                        new AndCondition()
                        {
                            Relations = new List<Relation>()
                            {
                                new InRelation()
                                {
                                    Expr = new Expr() { Operand = Operand.v },
                                    Ranges = new List<Range<int>>() { new Range<int>(0) }
                                },
                                new InRelation()
                                {
                                    Expr = new ModExpr()
                                    {
                                        Operand = Operand.i,
                                        Value = 10
                                    },
                                    Ranges = new List<Range<int>>()
                                    {
                                        new Range<int>(1)
                                    }
                                },
                                new InRelation()
                                {
                                    Not = true,
                                    Expr = new ModExpr()
                                    {
                                        Operand = Operand.i,
                                        Value = 100
                                    },
                                    Ranges = new List<Range<int>>()
                                    {
                                        new Range<int>(11)
                                    }
                                }
                            }
                        },
                        new AndCondition()
                        {
                            Relations = new List<Relation>()
                            {
                                new InRelation()
                                {
                                    Expr = new ModExpr()
                                    {
                                        Operand = Operand.f,
                                        Value = 10
                                    },
                                    Ranges = new List<Range<int>>()
                                    {
                                        new Range<int>(1)
                                    }
                                },
                                new InRelation()
                                {
                                    Not = true,
                                    Expr = new ModExpr()
                                    {
                                        Operand = Operand.f,
                                        Value = 100
                                    },
                                    Ranges = new List<Range<int>>()
                                    {
                                        new Range<int>(11)
                                    }
                                }
                            }
                        }
                    }
                },
                Name = null,
                Samples = new Samples()
                {
                    IntegerSamples = new List<Range<decimal>>()
                    {
                        new Range<decimal>(1M),
                        new Range<decimal>(21M),
                        new Range<decimal>(31M),
                        new Range<decimal>(41M),
                        new Range<decimal>(51M),
                        new Range<decimal>(61M),
                        new Range<decimal>(71M),
                        new Range<decimal>(81M),
                        new Range<decimal>(101M),
                        new Range<decimal>(1001M),
                    },
                    DecimalSamples = new List<Range<decimal>>()
                    {
                        new Range<decimal>(0.1M),
                        new Range<decimal>(1.1M),
                        new Range<decimal>(2.1M),
                        new Range<decimal>(3.1M),
                        new Range<decimal>(4.1M),
                        new Range<decimal>(5.1M),
                        new Range<decimal>(6.1M),
                        new Range<decimal>(7.1M),
                        new Range<decimal>(10.1M),
                        new Range<decimal>(100.1M),
                        new Range<decimal>(1000.1M)
                    }
                }
            };
            Rule rule = parser.Parse();
            rule.Should().BeEquivalentTo(expected);
        }

        [Test]
        public void RuleWithInvalidOperator()
        {
            var parser = new PParser("v < 0");
            Action a = () => parser.Parse();
            a.Should().Throw<PParseException>();
        }

        [Test]
        public void RuleWithNearlyOverflowingInteger()
        {
            var parser = new PParser("v = 2147483647");
            var expected = new Rule()
            {
                Condition = new Condition()
                {
                    Conditions = new List<AndCondition>()
                    {
                        new AndCondition()
                        {
                            Relations = new List<Relation>()
                            {
                                new InRelation()
                                {
                                    Expr = new Expr() { Operand = Operand.v },
                                    Ranges = new List<Range<int>>()
                                    {
                                        new Range<int>(2147483647)
                                    }
                                }
                            }
                        }
                    }
                }
            };
            var rule = parser.Parse();
            rule.Should().BeEquivalentTo(expected);
        }

        [Test]
        public void RuleWithOverflowingInteger()
        {
            var parser = new PParser("v = 2147483648");
            Action a = () => parser.Parse();
            a.Should().Throw<PParseException>();
        }

        [Test]
        public void IsRule()
        {
            var parser = new PParser("w is 6 and t is not 7");
            var expected = new Rule()
            {
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
                                    Expr = new Expr() { Operand = Operand.w },
                                    Value = 6
                                },
                                new IsRelation()
                                {
                                    Not = true,
                                    Expr = new Expr() { Operand = Operand.t },
                                    Value = 7
                                }
                            }
                        }
                    }
                }
            };
            parser.Parse().Should().BeEquivalentTo(expected);
        }

        [Test]
        public void WithinRule()
        {
            var parser = new PParser("t within 6..7,100 and n not within 1..1000");
            var expected = new Rule()
            {
                Condition = new Condition()
                {
                    Conditions = new List<AndCondition>()
                    {
                        new AndCondition()
                        {
                            Relations = new List<Relation>()
                            {
                                new WithinRelation()
                                {
                                    Expr = new Expr() { Operand = Operand.t },
                                    Ranges = new List<Range<int>>()
                                    {
                                        new Range<int>(6, 7),
                                        new Range<int>(100)
                                    }
                                },
                                new WithinRelation()
                                {
                                    Not = true,
                                    Expr = new Expr() { Operand = Operand.n },
                                    Ranges = new List<Range<int>>()
                                    {
                                        new Range<int>(1, 1000)
                                    }
                                }
                            }
                        }
                    }
                }
            };
            parser.Parse().Should().BeEquivalentTo(expected);
        }
    }
}
