using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Fluent.Net.SimpleExample
{
    public class TranslationService
    {
        IEnumerable<MessageContext> _contexts;

        public TranslationService(IEnumerable<MessageContext> contexts)
        {
            _contexts = contexts;
        }

        public static Dictionary<string, object> Args(string name, object value, params object[] args)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (args.Length % 2 != 0)
            {
                throw new ArgumentException("Expected a comma separated list " +
                    "of name, value arguments but the number of arguments is " +
                    "not a multiple of two", nameof(args));
            }

            var argsDic = new Dictionary<string, object>
            {
                { name, value }
            };

            for (int i = 0; i < args.Length; i += 2)
            {
                name = args[i] as string;
                if (String.IsNullOrEmpty(name))
                {
                    throw new ArgumentException("Expected the argument at " +
                        $"index {i} to be a non-empty string",
                        nameof(args));
                }
                value = args[i + 1];
                if (value == null)
                {
                    throw new ArgumentNullException("args",
                        $"Expected the argument at index {i + 1} " +
                        "to be a non-null value");
                }
                argsDic.Add(name, value);
            }

            return argsDic;
        }

        public string GetString(string id, IDictionary<string, object> args = null,
            ICollection<FluentError> errors = null)
        {
            foreach (var context in _contexts)
            {
                var msg = context.GetMessage(id);
                if (msg != null)
                {
                    return context.Format(msg, args, errors);
                }
            }
            return "";
        }

        public string PreferredLocale => _contexts.First().Locales.First();

        CultureInfo _culture;
        public CultureInfo Culture
        {
            get
            {
                if (_culture == null)
                {
                    _culture = new CultureInfo(PreferredLocale);
                }
                return _culture;
            }
        }
    }
}
