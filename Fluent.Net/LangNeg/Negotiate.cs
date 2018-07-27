using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fluent.Net.LangNeg
{
    public enum Strategy
    {
        Filtering,
        Matching,
        Lookup
    }

    public static class Negotiate
    {
        /// <summary>
        /// Negotiates the languages between the list of requested locales against
        /// a list of available locales.
        ///
        /// The algorithm is based on the BCP4647 3.3.2 Extended Filtering algorithm,
        /// with several modifications:
        ///
        ///  1) available locales are treated as ranges
        ///
        ///    This change allows us to match a more specific request against
        ///    more generic available locale.
        ///
        ///    For example, if the available locale list provides locale `en`,
        ///    and the requested locale is `en-US`, we treat the available locale as
        ///    a locale that matches all possible english requests.
        ///
        ///    This means that we expect available locale ID to be as precize as
        ///    the matches they want to cover.
        ///
        ///    For example, if there is only `sr` available, it's ok to list
        ///    it in available locales. But once the available locales has both,
        ///    Cyrl and Latn variants, the locale IDs should be `sr-Cyrl` and `sr-Latn`
        ///    to avoid any `sr-*` request to match against whole `sr` range.
        ///
        ///    What it does ([requested] * [available] = [supported]):
        ///
        ///    ['en-US'] * ['en'] = ['en']
        ///
        ///  2) likely subtags from LDML 4.3 Likely Subtags has been added
        ///
        ///    The most obvious likely subtag that can be computed is a duplication
        ///    of the language field onto region field (`fr` => `fr-FR`).
        ///
        ///    On top of that, likely subtags may use a list of mappings, that
        ///    allow the algorithm to handle non-obvious matches.
        ///    For example, making sure that we match `en` to `en-US` or `sr` to
        ///    `sr-Cyrl`, while `sr-RU` to `sr-Latn-RU`.
        ///
        ///    This list can be taken directly from CLDR Supplemental Data.
        ///
        ///    What it does ([requested] * [available] = [supported]):
        ///
        ///    ['fr'] * ['fr-FR'] = ['fr-FR']
        ///    ['en'] * ['en-US'] = ['en-US']
        ///    ['sr'] * ['sr-Latn', 'sr-Cyrl'] = ['sr-Cyrl']
        ///
        ///  3) variant/region range check has been added
        ///
        ///    Lastly, the last form of check is against the requested locale ID
        ///    but with the variant/region field replaced with a `*` range.
        ///
        ///    The rationale here laid out in LDML 4.4 Language Matching:
        ///      "(...) normally the fall-off between the user's languages is
        ///      substantially greated than regional variants."
        ///
        ///    In other words, if we can't match for the given region, maybe
        ///    we can match for the same language/script but other region, and
        ///    it will in most cases be preferred over falling back on the next
        ///    language.
        ///
        ///    What it does ([requested] * [available] = [supported]):
        ///
        ///    ['en-AU'] * ['en-US'] = ['en-US']
        ///    ['sr-RU'] * ['sr-Latn-RO'] = ['sr-Latn-RO'] // sr-RU -> sr-Latn-RU
        ///
        ///    It works similarly to getParentLocales algo, except that we stop
        ///    after matching against variant/region ranges and don't try to match
        ///    ignoring script ranges. That means that `sr-Cyrl` will never match
        ///    against `sr-Latn`.
        /// </summary>
        static SortedSet<string> FilterMatches(
            string[] requestedLocales,
            string[] availableLocales,
            Strategy strategy
        )
        {
            var supportedLocales = new SortedSet<string>();
            var availLocales = availableLocales.Select(l => new Locale(l, true)).ToList();

            foreach (var reqLocStr in requestedLocales)
            {
                var requestedLocale = new Locale(reqLocStr.ToLowerInvariant());
                if (requestedLocale.Language == null)
                {
                    continue;
                }

                // Attempt to make an exact match
                // Example: `en-US` === `en-US`
                foreach (var availableLocale in availableLocales)
                {
                    if (String.Equals(reqLocStr, availableLocale,
                      StringComparison.InvariantCultureIgnoreCase))
                    {
                        supportedLocales.Add(availableLocale);
                        for (int i = 0; i < availLocales.Count; ++i)
                        {
                            if (availLocales[i].Equals(requestedLocale))
                            {
                                availLocales.RemoveAt(i);
                                break;
                            }
                        }
                        if (strategy == Strategy.Lookup)
                        {
                            return supportedLocales;
                        }
                        else if (strategy == Strategy.Filtering)
                        {
                            continue;
                        }
                        else
                        {
                            goto outer;
                        }
                    }
                }

                // Attempt to match against the available range
                // This turns `en` into `en-*-*-*` and `en-US` into `en-*-US-*`
                // Example: ['en-US'] * ['en'] = ['en']
                for (var i = 0; i < availLocales.Count; ++i)
                {
                    if (requestedLocale.Matches(availLocales[i]))
                    {
                        supportedLocales.Add(availLocales[i].LocaleId);
                        availLocales.RemoveAt(i--);
                        if (strategy == Strategy.Lookup)
                        {
                            return supportedLocales;
                        }
                        else if (strategy == Strategy.Filtering)
                        {
                            continue;
                        }
                        else
                        {
                            goto outer;
                        }
                    }
                }

                // Attempt to retrieve a maximal version of the requested locale ID
                // If data is available, it'll expand `en` into `en-Latn-US` and
                // `zh` into `zh-Hans-CN`.
                // Example: ['en'] * ['en-GB', 'en-US'] = ['en-US']
                if (requestedLocale.AddLikelySubtags())
                {
                    for (int i = 0; i < availLocales.Count; ++i)
                    {
                        if (requestedLocale.Matches(availLocales[i]))
                        {
                            supportedLocales.Add(availLocales[i].LocaleId);
                            availLocales.RemoveAt(i--);
                            if (strategy == Strategy.Lookup)
                            {
                                return supportedLocales;
                            }
                            else if (strategy == Strategy.Filtering)
                            {
                                continue;
                            }
                            else
                            {
                                goto outer;
                            }
                        }
                    }
                }

                // Attempt to look up for a different variant for the same locale ID
                // Example: ['en-US-mac'] * ['en-US-win'] = ['en-US-win']
                requestedLocale.SetVariantRange();

                for (int i = 0; i < availLocales.Count; ++i)
                {
                    if (requestedLocale.Matches(availLocales[i]))
                    {
                        supportedLocales.Add(availLocales[i].LocaleId);
                        availLocales.RemoveAt(i--);
                        if (strategy == Strategy.Lookup)
                        {
                            return supportedLocales;
                        }
                        else if (strategy == Strategy.Filtering)
                        {
                            continue;
                        }
                        else
                        {
                            goto outer;
                        }
                    }
                }

                // Attempt to look up for a different region for the same locale ID
                // Example: ['en-US'] * ['en-AU'] = ['en-AU']
                requestedLocale.SetRegionRange();

                for (int i = 0; i < availLocales.Count; ++i)
                {
                    if (requestedLocale.Matches(availLocales[i])) 
                    {
                        supportedLocales.Add(availLocales[i].LocaleId);
                        availLocales.RemoveAt(i--);
                        if (strategy == Strategy.Lookup) 
                        {
                            return supportedLocales;
                        }
                        else if (strategy == Strategy.Filtering)
                        {
                            continue;
                        } 
                        else 
                        {
                            goto outer;
                        }
                    }
                }
                outer:;
            }

            return supportedLocales;
        }

        static readonly string[] s_emptyArray = new string[0];

        /// <summary>
        /// Negotiates the languages between the list of requested locales against
        /// a list of available locales.
        ///
        /// It accepts three arguments:
        ///
        ///   requestedLocales:
        ///     an Array of strings with BCP47 locale IDs sorted
        ///     according to user preferences.
        ///
        ///   availableLocales:
        ///     an Array of strings with BCP47 locale IDs of locale for which
        ///     resources are available. Unsorted.
        ///
        ///   options:
        ///     An object with the following, optional keys:
        ///
        ///       strategy: 'filtering' (default) | 'matching' | 'lookup'
        ///
        ///       defaultLocale:
        ///         a string with BCP47 locale ID to be used
        ///         as a last resort locale.
        ///
        ///
        /// It returns an Array of strings with BCP47 locale IDs sorted according to the
        /// user preferences.
        ///
        /// The exact list will be selected differently depending on the strategy:
        ///
        ///   'filtering': (default)
        ///     In the filtering strategy, the algorithm will attempt to match
        ///     as many keys in the available locales in order of the requested locales.
        ///
        ///   'matching':
        ///     In the matching strategy, the algorithm will attempt to find the
        ///     best possible match for each element of the requestedLocales list.
        ///
        ///   'lookup':
        ///     In the lookup strategy, the algorithm will attempt to find a single
        ///     best available locale based on the requested locales list.
        ///
        ///     This strategy requires defaultLocale option to be set.
        /// </summary>
        public static string[] NegotiateLanguages(
            string[] requestedLocales,
            string[] availableLocales,
            Strategy strategy = Strategy.Filtering,
            string defaultLocale = null
        )
        {
            if (strategy == Strategy.Lookup && String.IsNullOrEmpty(defaultLocale))
            {
                throw new
                  ArgumentException(nameof(defaultLocale),
                      "defaultLocale cannot be undefined for strategy `lookup`");
            }
            if (requestedLocales == null)
            {
                requestedLocales = s_emptyArray;
            }
            if (availableLocales == null)
            {
                availableLocales = s_emptyArray;
            }

            var supportedLocales = FilterMatches(
                requestedLocales, availableLocales, strategy);

            if (strategy == Strategy.Lookup)
            {
                if (supportedLocales.Count == 0)
                {
                    supportedLocales.Add(defaultLocale);
                }
            }
            else if (!String.IsNullOrEmpty(defaultLocale) && 
                !supportedLocales.Contains(defaultLocale, StringComparer.InvariantCultureIgnoreCase))
            {
                supportedLocales.Add(defaultLocale);
            }
            return supportedLocales.ToArray();
        }

        struct LangWithQ
        {
            public int Index { get; set; }
            public string Lang { get; set; }
            public double Q { get; set; }
        }

        public static IList<string> AcceptedLanguages(string acceptLanguageHeader)
        {
            var langsWithQ = new List<LangWithQ>();
            var index = 0;
            foreach (var acceptEntry in (acceptLanguageHeader ?? "").Split(new char[] { ',' },
                StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()))
            {
                var langWithQ = acceptEntry.Split(';');
                if (langWithQ.Length > 0)
                {
                    var q = 1.0;
                    if (langWithQ.Length > 1)
                    {
                        var qVal = langWithQ[1].Split('=').Select(x => x.Trim()).ToArray();
                        if (qVal.Length == 2 &&
                            String.Equals(qVal[0], "q", StringComparison.InvariantCultureIgnoreCase))
                        {
                            Double.TryParse(qVal[1], out q);
                        }
                    }
                    langsWithQ.Add(new LangWithQ
                    { Index = index++, Lang = langWithQ[0], Q = q });
                }
            }
            // order by q descending, keeping the header order for equal weights
            langsWithQ.Sort((a, b) => a.Q > b.Q ? -1 : a.Q < b.Q ? 1 : a.Index - b.Index);
            return langsWithQ.Select(x => x.Lang).ToList();
        }
    }
}
