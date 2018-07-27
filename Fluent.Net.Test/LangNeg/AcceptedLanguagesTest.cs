using Fluent.Net.LangNeg;
using FluentAssertions;
using NUnit.Framework;

namespace Fluent.Net.Test.LangNeg
{
    public class AcceptedLanguagesTest
    {
        [Test]
        public void WithNoQualityValues()
        {
            var actual = "en-US, fr, pl";
            var expected = new string[] { "en-US", "fr", "pl" };
            Negotiate.AcceptedLanguages(actual).Should().BeEquivalentTo(expected);

            actual = "sr-Latn";
            expected = new string[] { "sr-Latn" };
            Negotiate.AcceptedLanguages(actual).Should().BeEquivalentTo(expected);
        }

        [Test]
        public void WithQualityValues()
        {
            var actual = "fr-CH, fr;q=0.9, en;q=0.8, de;q=0.7, *;q=0.5";
            var expected = new string[] { "fr-CH", "fr", "en", "de", "*" };
            Negotiate.AcceptedLanguages(actual).Should().BeEquivalentTo(expected);
        }

        [Test]
        public void WithOutOfOrderQualityValues()
        {
            var actual = "en;q=0.8, fr;q=0.9, de;q=0.7, *;q=0.5, fr-CH";
            var expected = new string[] { "fr-CH", "fr", "en", "de", "*" };
            Negotiate.AcceptedLanguages(actual).Should().BeEquivalentTo(expected);
        }

        [Test]
        public void WithEqualQValues()
        {
            var actual = "en;q=0.1, fr;q=0.1, de;q=0.1, *;q=0.1";
            var expected = new string[] { "en", "fr", "de", "*" };
            Negotiate.AcceptedLanguages(actual).Should().BeEquivalentTo(expected);
        }

        [Test]
        public void WithDuffQValues()
        {
            var actual = "en;q=no, fr;z=0.9, de;q=0.7;q=9, *;q=0.5, fr-CH;q=a=0.1";
            var expected = new string[] { "en", "fr", "fr-CH", "de", "*" };
            Negotiate.AcceptedLanguages(actual).Should().BeEquivalentTo(expected);
        }

        [Test]
        public void WithEmptyEntries()
        {
            var actual = "en;q=0.8,,, fr;q=0.9,, de;q=0.7, *;q=0.5, fr-CH";
            var expected = new string[] { "fr-CH", "fr", "en", "de", "*" };
            Negotiate.AcceptedLanguages(actual).Should().BeEquivalentTo(expected);
        }

        [Test]
        public void WithNullHeader()
        {
            string actual = null;
            var expected = new string[0];
            Negotiate.AcceptedLanguages(actual).Should().BeEquivalentTo(expected);
        }

        [Test]
        public void WithEmptyHeader()
        {
            string actual = "";
            var expected = new string[0];
            Negotiate.AcceptedLanguages(actual).Should().BeEquivalentTo(expected);
        }
    }
}
