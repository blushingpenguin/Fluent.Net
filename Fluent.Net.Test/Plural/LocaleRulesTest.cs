using Fluent.Net.Plural;
using FluentAssertions;
using NUnit.Framework;

namespace Fluent.Net.Test.Plural
{
    public class LocaleRulesTest
    {
        [Test]
        public void CheckEnRules()
        {
            LocaleRules.Select("en", "1").Should().Be("one");
            LocaleRules.Select("en", "1.6").Should().Be("other");
            LocaleRules.Select("en", "1.0").Should().Be("other");
            LocaleRules.Select("en", "9").Should().Be("other");
        }

        [Test]
        public void CheckLtRules()
        {
            // <pluralRule count="one">n % 10 = 1 and n % 100 != 11..19 @integer 1, 21, 31, 41, 51, 61, 71, 81, 101, 1001, … @decimal 1.0, 21.0, 31.0, 41.0, 51.0, 61.0, 71.0, 81.0, 101.0, 1001.0, …</pluralRule>
            // <pluralRule count="few">n % 10 = 2..9 and n % 100 != 11..19 @integer 2~9, 22~29, 102, 1002, … @decimal 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 22.0, 102.0, 1002.0, …</pluralRule>
            // <pluralRule count="many">f != 0   @decimal 0.1~0.9, 1.1~1.7, 10.1, 100.1, 1000.1, …</pluralRule>
            // <pluralRule count="other"> @integer 0, 10~20, 30, 40, 50, 60, 100, 1000, 10000, 100000, 1000000, … @decimal 0.0, 10.0, 11.0, 12.0, 13.0, 14.0, 15.0, 16.0, 100.0, 1000.0, 10000.0, 100000.0, 1000000.0, …</pluralRule>

            LocaleRules.Select("lt", "1").Should().Be("one");
            LocaleRules.Select("lt", "11").Should().Be("other");
            LocaleRules.Select("lt", "21").Should().Be("one");
            LocaleRules.Select("lt", "91").Should().Be("one");
            LocaleRules.Select("lt", "101").Should().Be("one");

            LocaleRules.Select("lt", "2").Should().Be("few");
            LocaleRules.Select("lt", "37").Should().Be("few");
            LocaleRules.Select("lt", "9").Should().Be("few");
            LocaleRules.Select("lt", "10").Should().Be("other");
            LocaleRules.Select("lt", "23").Should().Be("few");

            LocaleRules.Select("lt", "0.6").Should().Be("many");
            LocaleRules.Select("lt", "1.1").Should().Be("many");
            LocaleRules.Select("lt", "2.0").Should().Be("few");
            LocaleRules.Select("lt", "1234.456").Should().Be("many");
            
            LocaleRules.Select("lt", "14").Should().Be("other");
            LocaleRules.Select("lt", "40").Should().Be("other");
        }
    }
}
