using Fluent.Net.Plural;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Fluent.Net.LangNeg;

namespace Fluent.Net.Test.LangNeg
{
    public class NegotiateTest
    {
        public class NegotiateTestCase
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Group { get; set; }
            public string[] RequestedLocales { get; set; }
            public string[] AvailableLocales { get; set; }
            public Strategy Strategy { get; set; }
            public string DefaultLocale { get; set; }
            public string[] Expected { get; set; }

            public override string ToString()
            {
                return Description;
            }
        }

        static string[] Strings(params string[] strings)
        {
            return strings;
        }

        // because of course, nobody would find typedef
        // in c# useful, it's so old world and not modern or anything
        static public Dictionary<Strategy, Dictionary<string, NegotiateTestCase[]>> TestData =
            new Dictionary<Strategy, Dictionary<string, NegotiateTestCase[]>>()
        {
            {
                Strategy.Filtering,
                new Dictionary<string, NegotiateTestCase[]>()
                {
                    {
                        "exact match",
                        new NegotiateTestCase[]
                        {
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("en"),
                                AvailableLocales = Strings("en"),
                                Expected = Strings("en")
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("en-US"),
                                AvailableLocales = Strings("en-US"),
                                Expected = Strings("en-US")
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("en-Latn-US"),
                                AvailableLocales = Strings("en-Latn-US"),
                                Expected = Strings("en-Latn-US")
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("en-Latn-US-mac"),
                                AvailableLocales = Strings("en-Latn-US-mac"),
                                Expected = Strings("en-Latn-US-mac")
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("fr-FR"),
                                AvailableLocales = Strings("de", "it", "fr-FR"),
                                Expected = Strings("fr-FR")
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("fr", "pl", "de-DE"),
                                AvailableLocales = Strings("pl", "en-US", "de-DE"),
                                Expected = Strings("pl", "de-DE")
                            }
                        }
                    },
                    {
                        "available as range",
                        new NegotiateTestCase[]
                        {
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("en-US"),
                                AvailableLocales = Strings("en"),
                                Expected = Strings("en")
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("en-Latn-US"),
                                AvailableLocales = Strings("en-US"),
                                Expected = Strings("en-US")
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("en-US-mac"),
                                AvailableLocales = Strings("en-US"),
                                Expected = Strings("en-US"),
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("fr-CA", "de-DE"),
                                AvailableLocales = Strings("fr", "it", "de"),
                                Expected = Strings("fr", "de"),
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("ja-JP-mac"),
                                AvailableLocales = Strings("ja"),
                                Expected = Strings("ja")
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("en-Latn-GB", "en-Latn-IN"),
                                AvailableLocales = Strings("en-IN", "en-GB"),
                                Expected = Strings("en-GB", "en-IN")
                            }
                        }
                    },
                    {
                        "should match on likely subtag",
                        new NegotiateTestCase[]
                        {
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("en"),
                                AvailableLocales = Strings("en-GB", "de", "en-US"),
                                Expected = Strings("en-US", "en-GB")
                            },
                            new NegotiateTestCase()
                            {
                                Name = "en (2)",
                                RequestedLocales = Strings("en"),
                                AvailableLocales = Strings("en-Latn-GB", "de", "en-Latn-US"),
                                Expected = Strings("en-Latn-US", "en-Latn-GB")
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("fr"),
                                AvailableLocales = Strings("fr-CA", "fr-FR"),
                                Expected = Strings("fr-FR", "fr-CA"),
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("az-IR"),
                                AvailableLocales = Strings("az-Latn", "az-Arab"),
                                Expected = Strings("az-Arab")
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("sr-RU"),
                                AvailableLocales = Strings("sr-Cyrl", "sr-Latn"),
                                Expected = Strings("sr-Latn"),
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("sr"),
                                AvailableLocales = Strings("sr-Latn", "sr-Cyrl"),
                                Expected = Strings("sr-Cyrl"),
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("zh-GB"),
                                AvailableLocales = Strings("zh-Hans", "zh-Hant"),
                                Expected = Strings("zh-Hant"),
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("sr", "ru"),
                                AvailableLocales = Strings("sr-Latn", "ru"),
                                Expected = Strings("ru"),
                            },
                            new NegotiateTestCase()
                            {
                                Name = "sr-RU (2)",
                                RequestedLocales = Strings("sr-RU"),
                                AvailableLocales = Strings("sr-Latn-RO", "sr-Cyrl"),
                                Expected = Strings("sr-Latn-RO"),
                            },
                        }
                    },
                    {
                        "should match on a requested locale as a range",
                        new NegotiateTestCase[]
                        {
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("en-*-US"),
                                AvailableLocales = Strings("en-US"),
                                Expected = Strings("en-US"),
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("en-Latn-US-*"),
                                AvailableLocales = Strings("en-Latn-US"),
                                Expected = Strings("en-Latn-US"),
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("en-*-US-*"),
                                AvailableLocales = Strings("en-US"),
                                Expected = Strings("en-US"),
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("*"),
                                AvailableLocales = Strings("de", "pl", "it", "fr", "ru"),
                                Expected = Strings("de", "pl", "it", "fr", "ru")
                            }
                        }
                    },
                    {
                        "should match cross-region",
                        new NegotiateTestCase[]
                        {
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("en"),
                                AvailableLocales = Strings("en-US"),
                                Expected = Strings("en-US"),
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("en-US"),
                                AvailableLocales = Strings("en-GB"),
                                Expected = Strings("en-GB"),
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("en-Latn-US"),
                                AvailableLocales = Strings("en-Latn-GB"),
                                Expected = Strings("en-Latn-GB"),
                            },
                            new NegotiateTestCase()
                            {
                                // This is a cross-region check, because the requested Locale
                                // is really lang: en, script: *, region: undefined
                                Name = "en-* cross-region check",
                                RequestedLocales = Strings("en-*"),
                                AvailableLocales = Strings("en-US"),
                                Expected = Strings("en-US"),
                            }
                        }
                    },
                    {
                        "should match cross-variant",
                        new NegotiateTestCase[]
                        {
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("en-US-mac"),
                                AvailableLocales = Strings("en-US-win"),
                                Expected = Strings("en-US-win"),
                            }
                        }
                    },
                    {
                        "should prioritize properly",
                        new NegotiateTestCase[]
                        {
                            new NegotiateTestCase()
                            {
                                // exact match first
                                Name = "en-US exact match first",
                                RequestedLocales = Strings("en-US"),
                                AvailableLocales = Strings("en-US-mac", "en", "en-US"),
                                Expected = Strings("en-US", "en", "en-US-mac"),
                            },
                            new NegotiateTestCase()
                            {
                                // available as range second
                                Name = "en-Latn-US available as range second",
                                RequestedLocales = Strings("en-Latn-US"),
                                AvailableLocales = Strings("en-GB", "en-US"),
                                Expected = Strings("en-US", "en-GB"),
                            },
                            new NegotiateTestCase()
                            {
                                // likely subtags third
                                Name = "en likely subtags third",
                                RequestedLocales = Strings("en"),
                                AvailableLocales = Strings("en-Cyrl-US", "en-Latn-US"),
                                Expected = Strings("en-Latn-US"),
                            },
                            new NegotiateTestCase()
                            {
                                // variant range fourth
                                Name = "en-US-mac variant range fourth",
                                RequestedLocales = Strings("en-US-mac"),
                                AvailableLocales = Strings("en-US-win", "en-GB-mac"),
                                Expected = Strings("en-US-win", "en-GB-mac"),
                            },
                            new NegotiateTestCase()
                            {
                                // regional range fifth
                                Name = "en-US-mac regional range fifth",
                                RequestedLocales = Strings("en-US-mac"),
                                AvailableLocales = Strings("en-GB-win"),
                                Expected = Strings("en-GB-win"),
                            },
                        }
                    },
                    {
                        "should prioritize properly (extra tests)",
                        new NegotiateTestCase[]
                        {
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("en-US"),
                                AvailableLocales = Strings("en-GB", "en"),
                                Expected = Strings("en", "en-GB"),
                            },
                        }
                    },
                    {
                        "should handle default locale properly",
                        new NegotiateTestCase[]
                        {
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("fr"),
                                AvailableLocales = Strings("de", "it"),
                                Expected = Strings()
                            },
                            new NegotiateTestCase()
                            {
                                Name = "fr (2)",
                                RequestedLocales = Strings("fr"),
                                AvailableLocales = Strings("de", "it"),
                                DefaultLocale = "en-US",
                                Expected = Strings("en-US")
                            },
                            new NegotiateTestCase()
                            {
                                Name = "fr (3)",
                                RequestedLocales = Strings("fr"),
                                AvailableLocales = Strings("de", "en-US"),
                                DefaultLocale = "en-US",
                                Expected = Strings("en-US")
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("fr", "de-DE"),
                                AvailableLocales = Strings("de-DE", "fr-CA"),
                                DefaultLocale = "en-US",
                                Expected= Strings("fr-CA", "de-DE", "en-US")
                            }
                        }
                    },
                    {
                        "should handle all matches on the 1st higher than any on the 2nd",
                        new NegotiateTestCase[]
                        {
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("fr-CA-mac", "de-DE"),
                                AvailableLocales = Strings("de-DE", "fr-FR-win"),
                                Expected = Strings("fr-FR-win", "de-DE"),
                            }
                        }
                    },
                    {
                        "should handle cases and underscores",
                        new NegotiateTestCase[]
                        {
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("fr_FR"),
                                AvailableLocales = Strings("fr-FR"),
                                Expected = Strings("fr-FR"),
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("fr_fr"),
                                AvailableLocales = Strings("fr-fr"),
                                Expected = Strings("fr-fr"),
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("fr_Fr"),
                                AvailableLocales = Strings("fr-fR"),
                                Expected = Strings("fr-fR"),
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("fr_lAtN_fr"),
                                AvailableLocales = Strings("fr-Latn-FR"),
                                Expected = Strings("fr-Latn-FR"),
                            },
                            new NegotiateTestCase()
                            {
                                Name = "fr_FR (2)",
                                RequestedLocales = Strings("fr_FR"),
                                AvailableLocales = Strings("fr_FR"),
                                Expected = Strings("fr_FR"),
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("fr-FR"),
                                AvailableLocales = Strings("fr_FR"),
                                Expected = Strings("fr_FR"),
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("fr_Cyrl_FR_mac"),
                                AvailableLocales = Strings("fr_Cyrl_fr-mac"),
                                Expected = Strings("fr_Cyrl_fr-mac"),
                            }
                        }
                    },
                    {
                        "should not crash on invalid input",
                        new NegotiateTestCase[]
                        {
                            new NegotiateTestCase()
                            {
                                Name = "null requested locales",
                                RequestedLocales = null,
                                AvailableLocales = Strings("fr-FR"),
                                Expected = Strings()
                            },
                            new NegotiateTestCase()
                            {
                                Name = "null available locales",
                                RequestedLocales = Strings("fr-FR"),
                                AvailableLocales = null,
                                Expected = Strings()
                            },
                            new NegotiateTestCase()
                            {
                                Name = "rubbish locale names",
                                RequestedLocales = Strings("2"),
                                AvailableLocales = Strings("ąóżł"),
                                Expected = Strings()
                            },
                        }
                    }
                }
            },
            {
                Strategy.Matching,
                new Dictionary<string, NegotiateTestCase[]>()
                {
                    {
                        "should match only one per requested",
                        new NegotiateTestCase[]
                        {
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("fr", "en"),
                                AvailableLocales = Strings("en-US", "fr-FR", "en", "fr"),
                                Expected = Strings("fr", "en")
                            },
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("*"),
                                AvailableLocales = Strings("fr", "de", "it", "ru", "pl"),
                                Expected = Strings("fr")
                            }
                        }
                    },
                }
            },
            {
                Strategy.Lookup,
                new Dictionary<string, NegotiateTestCase[]>()
                {
                    {
                        "should match only one",
                        new NegotiateTestCase[]
                        {
                            new NegotiateTestCase()
                            {
                                RequestedLocales = Strings("fr-FR", "en"),
                                AvailableLocales = Strings("en-US", "fr-FR", "en", "fr"),
                                DefaultLocale = "en-US",
                                Expected = Strings("fr-FR")
                            }
                        }
                    }
                }
            }
        };

        public static IEnumerable<NegotiateTestCase> GetTestCases()
        {
            foreach (var strategy in TestData)
            {
                foreach (var group in strategy.Value)
                {
                    foreach (var testCase in group.Value)
                    {
                        testCase.Strategy = strategy.Key;
                        testCase.Description = group.Key + " - " +
                            (String.IsNullOrEmpty(testCase.Name) ?
                                String.Join(",", testCase.RequestedLocales) :
                                testCase.Name);
                        yield return testCase;
                    }
                }
            }
        }

        [Test, TestCaseSource("GetTestCases")]
        public void Test(NegotiateTestCase test)
        {
            var actual = Negotiate.NegotiateLanguages(test.RequestedLocales,
                test.AvailableLocales, test.Strategy, test.DefaultLocale);
            actual.Should().BeEquivalentTo(test.Expected,
                options => options.WithStrictOrdering());
        }
    }
}
