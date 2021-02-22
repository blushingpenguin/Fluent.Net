using Fluent.Net.RuntimeAst;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Fluent.Net
{
    public class MessageContextOptions
    {
        public bool UseIsolating { get; set; }
        public Func<string, string> Transform { get; set; }
        public IDictionary<string, Resolver.ExternalFunction> Functions { get; set; }
    }

    /// <summary>
    /// Message contexts are single-language stores of translations.  They are
    /// responsible for parsing translation resources in the Fluent syntax and can
    /// format translation units (entities) to strings.
    /// 
    /// Always use `MessageContext.format` to retrieve translation units from a
    /// context.Translations can contain references to other entities or variables,
    /// conditional logic in form of select expressions, traits which describe their
    /// grammatical features, and can use Fluent builtins which make use of the
    /// `Intl` formatters to format numbers, dates, lists and more into the
    /// context's language. See the documentation of the Fluent syntax for more
    /// information.
    /// </summary>
    public class MessageContext
    {
        readonly static IDictionary<string, Resolver.ExternalFunction> s_emptyFunctions = new
            Dictionary<string, Resolver.ExternalFunction>();
        
        public IReadOnlyList<CultureInfo> Cultures { get; }

        [Obsolete("Access Cultures instead")]
        public IEnumerable<string> Locales => Cultures.Select(c => c.IetfLanguageTag);
        
        internal Dictionary<string, Message> _messages = new Dictionary<string, Message>();
        internal Dictionary<string, Message> _terms = new Dictionary<string, Message>();
        public Func<string, string> Transform { get; private set; }
        public bool UseIsolating { get; private set; } = true;
        public IDictionary<string, Resolver.ExternalFunction> Functions { get; private set; }
        public CultureInfo Culture { get; private set; }

        private static string NoOpTransform(string toTransform) => toTransform;

        /// <summary>
        /// Create an instance of `MessageContext`.
        /// 
        /// The `locales` argument is used to instantiate `Intl` formatters used by
        /// translations.  The `options` object can be used to configure the context.
        /// 
        /// Examples:
        /// 
        ///     const ctx = new MessageContext(locales);
        /// 
        ///     const ctx = new MessageContext(locales, { useIsolating: false });
        /// 
        ///     const ctx = new MessageContext(locales, {
        ///       useIsolating: true,
        ///       functions: {
        ///         NODE_ENV: () => process.env.NODE_ENV
        ///       }
        ///     });
        /// 
        /// Available options:
        /// 
        ///   - `functions` - an object of additional functions available to
        ///                   translations as builtins.
        /// 
        ///   - `useIsolating` - boolean specifying whether to use Unicode isolation
        ///                    marks (FSI, PDI) for bidi interpolations.
        /// 
        ///   - `transform` - a function used to transform string parts of patterns.
        /// </summary>
        /// <param name="locales">Locale or locales of the context</param>
        /// <param name="options">[options]</param>
        ///
        public MessageContext(
            IEnumerable<string>     locales, 
            MessageContextOptions   options = null
        ) : this(locales.Select(l => new CultureInfo(l)), options)
        {
        }

        public MessageContext(IEnumerable<CultureInfo> cultures, MessageContextOptions options = null)
        {
            Cultures = cultures.ToArray();
            Culture = Cultures[0];
            if (options != null)
            {
                UseIsolating = options.UseIsolating;
            }
            Transform = options?.Transform ?? NoOpTransform;
            Functions = new Dictionary<string, Resolver.ExternalFunction>(options?.Functions ?? s_emptyFunctions);
        }
        
        public MessageContext(
            CultureInfo culture,
            MessageContextOptions options = null
        ) : this(new [] { culture }, options)
        {
        }
        
        public MessageContext(
            string locale,
            MessageContextOptions options = null
        ) : this(new CultureInfo(locale), options)
        {
        }
        
        /// <summary>
        /// All available messages in the context.
        /// </summary>
        private IReadOnlyDictionary<string, Message> Messages => _messages;

        /// <summary>
        /// Check if a message is present in the context.
        ///</summary>
        ///
        /// <param name="id">The identifier of the message to check</param>
        /// 
        /// @returns {bool}
        ///
        public bool HasMessage(string id)
        {
            return _messages.ContainsKey(id);
        }

        /// <summary>
        /// Return the internal representation of a message.
        /// 
        /// The internal representation should only be used as an argument to
        /// `MessageContext.format`.
        /// </summary>
        /// @param {string} id - The identifier of the message to check.
        /// @returns {Any}
        /// 
        public Message GetMessage(string id)
        {
            Message message;
            _messages.TryGetValue(id, out message);
            return message;
        }

        /// <summary>
        /// Add a translation resource to the context.
        /// 
        /// The translation resource must use the Fluent syntax.  It will be parsed by
        /// the context and each translation unit (message) will be available in the
        /// context by its identifier.
        /// 
        ///     ctx.addMessages('foo = Foo');
        ///     ctx.getMessage('foo');
        /// 
        ///     // Returns a raw representation of the 'foo' message.
        /// 
        /// Parsed entities should be formatted with the `format` method in case they
        /// contain logic (references, select expressions etc.).
        /// </summary>
        /// @param   {string} source - Text resource with translations.
        /// @returns {Array<Error>}
        /// 
        public IList<ParseException> AddMessages(TextReader source)
        {
            return AddResource(FluentResource.FromReader(source));
        }

        public IList<ParseException> AddMessages(string source)
        {
            using (var sr = new StringReader(source))
            {
                return AddMessages(sr);
            }
        }

        /// <summary>
        /// Add a translation resource to the context.
        /// 
        /// The translation resource must be a proper FluentResource
        /// parsed by `MessageContext.parseResource`.
        /// 
        ///     let res = MessageContext.parseResource("foo = Foo");
        ///     ctx.addResource(res);
        ///     ctx.getMessage('foo');
        /// 
        ///     // Returns a raw representation of the 'foo' message.
        /// 
        /// Parsed entities should be formatted with the `format` method in case they
        /// contain logic (references, select expressions etc.).
        /// </summary>
        /// <param name="resource">The resource object</param>
        /// <returns>A list of errors encountered during adding or parsing the resource</returns>
        public IList<ParseException> AddResource(FluentResource resource)
        {
            var errors = new List<ParseException>(resource.Errors);
            foreach (var entry in resource.Entries)
            {
#if FALSE
                if (!(entry is Ast.MessageTermBase))
                {
                    // Ast.Comment or Ast.Junk
                    continue;
                }

                if (entry is Ast.Term t)
                {
                    if (_terms.ContainsKey(t.Id.Name))
                    {
                        errors.Add($"Attempt to override an existing term: \"{t.Id.Name}\"");
                        continue;
                    }
                    _terms.Add(t.Id.Name, t);
                }
                else if (entry is Ast.Message m)
                {
                    if (_messages.ContainsKey(m.Id.Name))
                    {
                        errors.Add($"Attempt to override an existing message: \"{m.Id.Name}\"");
                        continue;
                    }
                    _messages.Add(m.Id.Name, m);
                }
                // else Ast.Comment or Ast.Junk
#endif
                if (entry.Key.StartsWith("-"))
                {
                    if (_terms.ContainsKey(entry.Key))
                    {
                        errors.Add(new OverrideError(
                            $"Attempt to override an existing term: \"{entry.Key}\""));
                        continue;
                    }
                    _terms.Add(entry.Key, entry.Value);
                }
                else
                {
                    if (_messages.ContainsKey(entry.Key))
                    {
                        errors.Add(new OverrideError(
                            $"Attempt to override an existing message: \"{entry.Key}\""));
                        continue;
                    }
                    _messages.Add(entry.Key, entry.Value);
                }
            }
            return errors;
        }

        /// <summary>
        /// Format a message to a string or null.
        /// 
        /// Format a raw `message` from the context into a string (or a null if it has
        /// a null value).  `args` will be used to resolve references to variables
        /// passed as arguments to the translation.
        /// 
        /// In case of errors `format` will try to salvage as much of the translation
        /// as possible and will still return a string.  For performance reasons, the
        /// encountered errors are not returned but instead are appended to the
        /// `errors` array passed as the third argument.
        /// 
        ///     const errors = [];
        ///     ctx.addMessages('hello = Hello, { $name }!');
        ///     const hello = ctx.getMessage('hello');
        ///     ctx.format(hello, { name: 'Jane' }, errors);
        /// 
        ///     // Returns 'Hello, Jane!' and `errors` is empty.
        /// 
        ///     ctx.format(hello, undefined, errors);
        /// 
        ///     // Returns 'Hello, name!' and `errors` is now:
        /// 
        ///     [<ReferenceError: Unknown variable: name>]
        /// </summary>
        /// @param   {Object | string}    message
        /// @param   {Object | undefined} args
        /// @param   {Array}              errors
        /// @returns {?string}
        /// 
        public string Format(
            Node message,
            IDictionary<string, object> args = null,
            ICollection<FluentError> errors = null
        )
        {
            // TODO: do we even produce these? do we need to do the runtime parser as well?
            // optimize entities which are simple strings with no attributes
            // if (typeof message === "string") {
            //   return _transform(message);
            // }

            // optimize simple-string entities with attributes
            // if (typeof message.val === "string") {
            //   return _transform(message.val);
            // }

            // optimize entities with null values
            // if (message.val === undefined) {
            //   return null;
            // }

            // return resolve(this, args, message, errors);
            return Resolver.Resolve(this, message, args, errors);
        }
    }
}
