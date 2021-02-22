using Fluent.Net.Plural;
using FluentAssertions;
using NUnit.Framework;

namespace Fluent.Net.Test.Plural
{
    public class LocaleRulesTest
    {
        [Test]
        [TestCase("en", "1", ExpectedResult = "one")]
        [TestCase("en-US", "1", ExpectedResult = "one")]
        [TestCase("en", "1.6", ExpectedResult = "other")]
        [TestCase("en", "1.0", ExpectedResult = "other")]
        [TestCase("en", "9", ExpectedResult = "other")]
        // for lt:
        // <pluralRule count="one">n % 10 = 1 and n % 100 != 11..19 @integer 1, 21, 31, 41, 51, 61, 71, 81, 101, 1001, … @decimal 1.0, 21.0, 31.0, 41.0, 51.0, 61.0, 71.0, 81.0, 101.0, 1001.0, …</pluralRule>
        // <pluralRule count="few">n % 10 = 2..9 and n % 100 != 11..19 @integer 2~9, 22~29, 102, 1002, … @decimal 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 22.0, 102.0, 1002.0, …</pluralRule>
        // <pluralRule count="many">f != 0   @decimal 0.1~0.9, 1.1~1.7, 10.1, 100.1, 1000.1, …</pluralRule>
        // <pluralRule count="other"> @integer 0, 10~20, 30, 40, 50, 60, 100, 1000, 10000, 100000, 1000000, … @decimal 0.0, 10.0, 11.0, 12.0, 13.0, 14.0, 15.0, 16.0, 100.0, 1000.0, 10000.0, 100000.0, 1000000.0, …</pluralRule>
        [TestCase("lt", "1", ExpectedResult = "one")]
        [TestCase("lt", "11", ExpectedResult = "other")]
        [TestCase("lt", "21", ExpectedResult = "one")]
        [TestCase("lt", "91", ExpectedResult = "one")]
        [TestCase("lt", "101", ExpectedResult = "one")]
        [TestCase("lt", "2", ExpectedResult = "few")]
        [TestCase("lt", "37", ExpectedResult = "few")]
        [TestCase("lt", "9", ExpectedResult = "few")]
        [TestCase("lt", "10", ExpectedResult = "other")]
        [TestCase("lt", "23", ExpectedResult = "few")]
        [TestCase("lt", "0.6", ExpectedResult = "many")]
        [TestCase("lt", "1.1", ExpectedResult = "many")]
        [TestCase("lt", "2.0", ExpectedResult = "few")]
        [TestCase("lt", "1234.456", ExpectedResult = "many")]
        [TestCase("lt", "14", ExpectedResult = "other")]
        [TestCase("lt", "40", ExpectedResult = "other")]
        public string TestPluralSelect(string locale, string num)
        {
            return LocaleRules.Select(locale, num);
        }
    }
}
