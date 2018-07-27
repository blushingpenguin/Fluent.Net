using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Fluent.Net.LangNeg
{
    public class Locale
    {
        const string languageCodeRe = "([a-z]{2,3}|\\*)";
        const string scriptCodeRe = "(?:-([a-z]{4}|\\*))";
        const string regionCodeRe = "(?:-([a-z]{2}|\\*))";
        const string variantCodeRe = "(?:-([a-z]{3}|\\*))";

        /**
         * Regular expression splitting locale id into four pieces:
         *
         * Example: `en-Latn-US-mac`
         *
         * language: en
         * script:   Latn
         * region:   US
         * variant:  mac
         *
         * It can also accept a range `*` character on any position.
         */
        static readonly Regex s_localeRe = new Regex(
            $"^{languageCodeRe}{scriptCodeRe}?{regionCodeRe}?{variantCodeRe}?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string Language { get; set; }
        public string Script { get; set; }
        public string Region { get; set; }
        public string Variant { get; set; }
        public string LocaleId { get; set; }

        static string Coalesce(params string[] strings)
        {
            return strings.FirstOrDefault(s => !String.IsNullOrEmpty(s));
        }

        // for testing
        internal Locale() { }

        /**
         * Parses a locale id using the localeRe into an array with four elements.
         *
         * If the second argument `range` is set to true, it places range `*` char
         * in place of any missing piece.
         *
         * It also allows skipping the script section of the id, so `en-US` is
         * properly parsed as `en-*-US-*`.
         */
        public Locale(string locale, bool range = false)
        {
            if (String.IsNullOrEmpty(locale))
            {
                throw new ArgumentNullException(nameof(locale));
            }
            var result = s_localeRe.Match(locale.Replace("_", "-"));
            if (!result.Success)
            {
                return;
            }

            string missing = range ? "*" : null;

            Language = Coalesce(result.Groups[1].Value, missing);
            Script = Coalesce(result.Groups[2].Value, missing);
            Region = Coalesce(result.Groups[3].Value, missing);
            Variant = Coalesce(result.Groups[4].Value, missing);
            LocaleId = locale;
        }

        bool Matches(string a, string b)
        {
            return a == "*" || b == "*" ||
                (String.IsNullOrEmpty(a) && String.IsNullOrEmpty(b)) ||
                String.Equals(a, b, StringComparison.InvariantCultureIgnoreCase);
        }

        public bool Matches(Locale other)
        {
            return
                Matches(Language, other.Language) &&
                Matches(Script, other.Script) &&
                Matches(Region, other.Region) &&
                Matches(Variant, other.Variant);
        }

        public override int GetHashCode()
        {
            int hashCode = (Language ?? "").GetHashCode();
            hashCode = (hashCode * 397) ^ (Script ?? "").GetHashCode();
            hashCode = (hashCode * 397) ^ (Region ?? "").GetHashCode();
            hashCode = (hashCode * 397) ^ (Variant ?? "").GetHashCode();
            hashCode = (hashCode * 397) ^ (LocaleId ?? "").GetHashCode();
            return hashCode;
        }

        public override bool Equals(object obj)
        {
            if (obj is Locale other)
            {
                return Language == other.Language && Script == other.Script &&
                    Region == other.Region && Variant == other.Variant;
            }
            return false;
        }

        public void SetVariantRange()
        {
            Variant = "*";
        }

        public void SetRegionRange()
        {
            Region = "*";
        }

        public bool AddLikelySubtags()
        {
            var newLocale = LikelySubTags.GetLikelySubtagsMin(LocaleId.ToLowerInvariant());
            if (newLocale != null)
            {
                Language = newLocale.Language;
                Script = newLocale.Script;
                Region = newLocale.Region;
                Variant = newLocale.Variant;
                LocaleId = newLocale.LocaleId;
                return true;
            }
            return false;
        }
    }
}
