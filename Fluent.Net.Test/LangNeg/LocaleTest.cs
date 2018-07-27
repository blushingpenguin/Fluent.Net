using Fluent.Net.LangNeg;
using FluentAssertions;
using NUnit.Framework;

namespace Fluent.Net.Test.LangNeg
{
    public class LocaleTest
    {
        static void CheckEqual(string str, Locale ref_)
        {
            var result = new Locale(str).Equals(ref_);
            result.Should().BeTrue();
        }

        [Test]
        public void CanParseLanguagePart()
        {
            var ref_ = new Locale() { Language = "en" };
            CheckEqual("en", ref_);

            ref_ = new Locale() { Language = "lij" };
            CheckEqual("lij", ref_);
        }

        [Test]
        public void CanParseScriptPart()
        {
            var ref_ = new Locale() { Language = "en", Script = "Latn" };
            CheckEqual("en-Latn", ref_);

            ref_ = new Locale() { Language = "lij", Script = "Arab" };
            CheckEqual("lij-Arab", ref_);
        }

        [Test]
        public void CanParseRegionPart()
        {
            var ref_ = new Locale()
            {
                Language = "en",
                Script = "Latn",
                Region = "US"
            };
            CheckEqual("en-Latn-US", ref_);

            ref_ = new Locale()
            {
                Language = "lij",
                Script = "Arab",
                Region = "FA"
            };
            CheckEqual("lij-Arab-FA", ref_);
        }

        [Test]
        public void CanParseVariantPart()
        {
            var ref_ = new Locale()
            {
                Language = "en",
                Script = "Latn",
                Region = "US",
                Variant = "mac"
            };
            CheckEqual("en-Latn-US-mac", ref_);

            ref_ = new Locale()
            {
                Language = "lij",
                Script = "Arab",
                Region = "FA",
                Variant = "lin"
            };
            CheckEqual("lij-Arab-FA-lin", ref_);
        }

        [Test]
        public void CanSkipScriptPart()
        {
            var ref_ = new Locale() { Language = "en", Region = "US" };
            CheckEqual("en-US", ref_);

            ref_ = new Locale()
            {
                Language = "lij",
                Region = "FA",
                Variant = "lin"
            };
            CheckEqual("lij-FA-lin", ref_);
        }

        // XXX: this test is exactly the same as skipping script
        [Test]
        public void CanSkipVariantPart()
        {
            var ref_ = new Locale() { Language = "en", Region = "US" };
            CheckEqual("en-US", ref_);

            ref_ = new Locale()
            {
                Language = "lij",
                Region = "FA",
                Variant = "lin"
            };
            CheckEqual("lij-FA-lin", ref_);
        }

        [Test]
        public void CanParseLanguageRanges()
        {
            var ref_ = new Locale() { Language = "*" };
            CheckEqual("*", ref_);

            ref_ = new Locale() { Language = "*", Script = "Latn" };
            CheckEqual("*-Latn", ref_);

            ref_ = new Locale() { Language = "*", Region = "US" };
            CheckEqual("*-US", ref_);
        }

        [Test]
        public void CanParseScriptRanges()
        {
            var ref_ = new Locale() { Language = "en", Script = "*" };
            CheckEqual("en-*", ref_);

            ref_ = new Locale() { Language = "en", Script = "*", Region = "US" };
            CheckEqual("en-*-US", ref_);
        }

        [Test]
        public void CanParseRegionRanges()
        {
            var ref_ = new Locale()
            {
                Language = "en",
                Script = "Latn",
                Region = "*"
            };
            CheckEqual("en-Latn-*", ref_);
        }

        [Test]
        public void CanParseVariantRanges()
        {
            var ref_ = new Locale()
            {
                Language = "en",
                Script = "Latn",
                Region = "US",
                Variant = "*"
            };
            CheckEqual("en-Latn-US-*", ref_);
        }
    }
}
