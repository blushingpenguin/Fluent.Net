using Fluent.Net.RuntimeAst;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Fluent.Net
{
    public class MessageContextOptions
    {
        public bool UseIsolating { get; set; }
        public Func<string, string> Transform { get; set; }
        public IDictionary<string, Resolver.ExternalFunction> Functions { get; set; }
    }

    static class BuiltIns
    {
        public static FluentType Number(IList<object> args, IDictionary<string, object> options)
        {
            // TODO: add to errors?  what we doin here?
            if (args.Count != 1)
            {
                throw new Exception("Too many arguments to NUMBER() function");
            }
            return new FluentNumber(args[0].ToString());
        }

        public static FluentType DateTime(IList<object> args, IDictionary<string, object> options)
        {
            return new FluentString(args[0].ToString());
        }
    }

    public class ResolverEnvironment
    {
        public ICollection<FluentError> Errors { get; set; }
        public IDictionary<string, object> Arguments { get; set; }
        public MessageContext Context { get; set; }
        public HashSet<Pattern> Dirty { get; set; } = new HashSet<Pattern>();
    }

    public abstract class FluentError
    {
        public string Message { get; set; }

        public FluentError(string message)
        {
            Message = message;
        }
    }

    class RangeError : FluentError
    {
        public RangeError(string message) :
            base(message)
        {
        }
    }

    class TypeError : FluentError
    {
        public TypeError(string message) :
            base(message)
        {
        }
    }

    class ReferenceError : FluentError
    {
        public ReferenceError(string message) :
            base(message)
        {
        }
    }

    class OverrideError : ParseException
    {
        public OverrideError(string message) :
            base(message)
        {
        }
    }

    public interface IFluentType
    {
        string Value { get; set; }
        string Format(MessageContext ctx);
        bool Match(MessageContext ctx, object obj);
    }

    /**
     * The `FluentType` class is the base of Fluent's type system.
     *
     * Fluent types wrap JavaScript values and store additional configuration for
     * them, which can then be used in the `toString` method together with a proper
     * `Intl` formatter.
     */
    public abstract class FluentType : IFluentType
    {
        public string Value { get; set; }

        /**
         * Create an `FluentType` instance.
         *
         * @param   {Any}    value - JavaScript value to wrap.
         * @param   {Object} opts  - Configuration.
         * @returns {FluentType}
         */
        public FluentType(string value = null)
        {
            Value = value;
        }

        /**
         * Unwrap the raw value stored by this `FluentType`.
         *
         * @returns {Any}
        public string ValueOf()
        {
            return Value;
        }
         */

        /**
         * Format this instance of `FluentType` to a string.
         *
         * Formatted values are suitable for use outside of the `MessageContext`.
         * This method can use `Intl` formatters memoized by the `MessageContext`
         * instance passed as an argument.
         *
         * @param   {MessageContext} [ctx]
         * @returns {string}
         */
        public abstract string Format(MessageContext ctx);
        public abstract bool Match(MessageContext ctx, object obj);
    }

    public class FluentNone : Node, IFluentType
    {
        public string Value { get; set; }

        public FluentNone(string value = null)
        {
            Value = value;
        }

        public string Format(MessageContext ctx)
        {
            return !String.IsNullOrEmpty(Value) ? Value : "???";
        }

        public bool Match(MessageContext ctx, object other)
        {
            return other is FluentNone;
        }
    }

    public class FluentString : FluentType
    {
        public FluentString(string value) :
            base(value)
        {
        }

        public override string Format(MessageContext ctx)
        {
            return Value;
        }

        public override bool Match(MessageContext ctx, object other)
        {
            if (other is FluentString str)
            {
                return str.Value == Value;
            }
            return false;
        }
    }

    public class FluentNumber : FluentType
    {
        double _numberValue;

        public FluentNumber(string value) :
            base(value)
        {
            _numberValue = Double.Parse(value);
        }

        public override string Format(MessageContext ctx)
        {
            // TODO: match js number formattiing here
            // System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo(
            return String.Format(ctx.Culture, "{0}", _numberValue);
        }

        /**
         * Compare the object with another instance of a FluentType.
         *
         * @param   {MessageContext} ctx
         * @param   {FluentType}     other
         * @returns {bool}
         */
        public override bool Match(MessageContext ctx, object other)
        {
            if (other is FluentNumber n)
            {
                return _numberValue == n._numberValue;
            }
            return false;
        }
    }

    public class FluentDateTime : FluentType
    {
        DateTime _dateValue;

        public FluentDateTime(DateTime value) :
            base(value.ToString("o"))
        {
            _dateValue = value;
        }

        public override string Format(MessageContext ctx)
        {
            // TODO: match js number formattiig here?
            // System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo(
            return String.Format(ctx.Culture, "{0}", _dateValue);
        }

        public override bool Match(MessageContext ctx, object other)
        {
            if (other is FluentDateTime d)
            {
                return _dateValue == d._dateValue;
            }
            return false;
        }
    }

    public class FluentSymbol : FluentType
    {
        public FluentSymbol(string value) :
            base(value)
        {
        }

        public override string Format(MessageContext ctx)
        {
            return Value;
        }

        /**
         * Compare the object with another instance of a FluentType.
         *
         * @param   {MessageContext} ctx
         * @param   {FluentType}     other
         * @returns {bool}
         */
        public override bool Match(MessageContext ctx, object other)
        {
            if (other is FluentSymbol symbol)
            {
                return Value == symbol.Value;
            }
            else if (other is string str)
            {
                return Value == str;
            }
            else if (other is FluentString fstr)
            {
                return Value == fstr.Value;
            }
            else if (other is FluentNumber fnum)
            {
                return Value == Plural.LocaleRules.Select(ctx.Locales, fnum.Value);
            }
            return false;
        }
    }

    public static class Resolver
    {
        public delegate FluentType ExternalFunction(IList<object> args,
            IDictionary<string, object> options);

        static public IDictionary<string, ExternalFunction> BuiltInFunctions { get; } =
            new Dictionary<string, ExternalFunction>() {
            { "NUMBER", BuiltIns.Number },
            { "DATETIME", BuiltIns.DateTime }
        };

        // Prevent expansion of too long placeables.
        const int MAX_PLACEABLE_LENGTH = 2500;

        // Unicode bidi isolation characters.
        const char FSI = '\u2068';
        const char PDI = '\u2069';

        // 
        // Resolve expression to a Fluent type.
        // 
        // JavaScript strings are a special case.  Since they natively have the
        // `toString` method they can be used as if they were a Fluent type without
        // paying the cost of creating a instance of one.
        // 
        // @param   {Object} env
        //    Resolver environment object.
        // @param   {Object} expr
        //    An expression object to be resolved into a Fluent type.
        // @returns {FluentType}
        // @private
        // 
        static IFluentType ResolveNode(ResolverEnvironment env, Node expr)
        {
            // A fast-path for strings which are the most common case, and for
            // `FluentNone` which doesn't require any additional logic.
            if (expr is StringExpression se)
            {
                return new FluentString(env.Context.Transform(se.Value));
            }
            if (expr is FluentNone none)
            {
                return none;
            }

            if (expr is Message msg)
            {
                return ResolveNode(env, msg.Value);
            }
            if (expr is Pattern p)
            {
                return Pattern(env, p);
            }
            if (expr is VariantName varName)
            {
                return new FluentSymbol(varName.Name);
            }
            if (expr is NumberExpression num)
            {
                return new FluentNumber(num.Value);
            }
            if (expr is ExternalArgument arg)
            {
                return ExternalArgument(env, arg);
            }
            //                case "fun":
            //                    return FunctionReference(env, expr);
            if (expr is CallExpression call)
            {
                return CallExpression(env, call);
            }
            if (expr is MessageReference ref_)
            {
                var msgRef = MessageReference(env, ref_);
                return ResolveNode(env, msgRef);
            }
            if (expr is AttributeExpression attrExpr)
            {
                var attr = AttributeExpression(env, attrExpr);
                return ResolveNode(env, attr);
            }
            if (expr is VariantExpression varExpr)
            {
                var variant = VariantExpression(env, varExpr);
                return ResolveNode(env, variant);
            }
            if (expr is SelectExpression sel)
            {
                var member = SelectExpression(env, sel);
                return ResolveNode(env, member);
            }
            env.Errors.Add(new RangeError("No value"));
            return new FluentNone();
        }


        /**
         * Helper for choosing the default value from a set of members.
         *
         * Used in SelectExpressions and Type.
         *
         * @param   {Object} env
         *    Resolver environment object.
         * @param   {Object} members
         *    Hash map of variants from which the default value is to be selected.
         * @param   {Number} def
         *    The index of the default variant.
         * @returns {FluentType}
         * @private
         */
        static Node DefaultMember(ResolverEnvironment env, IList<Variant> members, int? def)
        {
            if (def.HasValue && def >= 0 && def < members.Count)
            {
                return members[def.Value].Value;
            }

            env.Errors.Add(new RangeError("No default"));
            return new FluentNone();
        }

        /**
         * Resolve a reference to another message.
         *
         * @param   {Object} env
         *    Resolver environment object.
         * @param   {Object} id
         *    The identifier of the message to be resolved.
         * @param   {String} id.name
         *    The name of the identifier.
         * @returns {FluentType}
         * @private
         */

        static Node MessageReference(ResolverEnvironment env, MessageReference ref_)
        {
            bool isTerm = ref_.Name.StartsWith("-");
            Message message;
            (isTerm ? env.Context._terms
                    : env.Context._messages).TryGetValue(ref_.Name, out message);

            if (message == null)
            {
                var err = isTerm
                    ? new ReferenceError($"Unknown term: {ref_.Name}")
                    : new ReferenceError($"Unknown message: ${ref_.Name}");
                env.Errors.Add(err);
                return new FluentNone(ref_.Name);
            }

            return message;
        }

        /**
         * Resolve a variant expression to the variant object.
         *
         * @param   {Object} env
         *    Resolver environment object.
         * @param   {Object} expr
         *    An expression to be resolved.
         * @param   {Object} expr.id
         *    An Identifier of a message for which the variant is resolved.
         * @param   {Object} expr.id.name
         *    Name a message for which the variant is resolved.
         * @param   {Object} expr.key
         *    Variant key to be resolved.
         * @returns {FluentType}
         * @private
         */
        static Node VariantExpression(ResolverEnvironment env, VariantExpression expr)
        {
            var message = MessageReference(env, expr.Id);
            if (message is FluentNone)
            {
                return message;
            }

            var keyword = (IFluentType)ResolveNode(env, expr.Key);

            if (message is SelectExpression sexp)
            {
                // Match the specified key against keys of each variant, in order.
                foreach (var variant in sexp.Variants)
                {
                    var key = ResolveNode(env, variant.Key);
                    if (keyword.Match(env.Context, keyword))
                    {
                        return variant.Value;
                    }
                }
            }

            env.Errors.Add(new ReferenceError(
                $"Unknown variant: {keyword.Format(env.Context)}"));
            return message;
        }

        /**
         * Resolve an attribute expression to the attribute object.
         *
         * @param   {Object} env
         *    Resolver environment object.
         * @param   {Object} expr
         *    An expression to be resolved.
         * @param   {String} expr.id
         *    An ID of a message for which the attribute is resolved.
         * @param   {String} expr.name
         *    Name of the attribute to be resolved.
         * @returns {FluentType}
         * @private
         */
        static Node AttributeExpression(ResolverEnvironment env, AttributeExpression expr)
        {
            var message = MessageReference(env, expr.Id);
            if (message is FluentNone)
            {
                return message;
            }

            var messageNode = (Message)message;
            if (messageNode.Attributes != null)
            {
                // Match the specified name against keys of each attribute.
                Node value;
                if (messageNode.Attributes.TryGetValue(expr.Name, out value))
                {
                    return value;
                }
            }

            env.Errors.Add(new ReferenceError($"Unknown attribute: {expr.Name}"));
            return message;
        }

        /**
         * Resolve a select expression to the member object.
         *
         * @param   {Object} env
         *    Resolver environment object.
         * @param   {Object} expr
         *    An expression to be resolved.
         * @param   {String} expr.exp
         *    Selector expression
         * @param   {Array} expr.vars
         *    List of variants for the select expression.
         * @param   {Number} expr.def
         *    Index of the default variant.
         * @returns {FluentType}
         * @private
         */
        static Node SelectExpression(ResolverEnvironment env, SelectExpression sexp)
        {
            if (sexp.Expression == null)
            {
                return DefaultMember(env, sexp.Variants, sexp.DefaultIndex);
            }

            var selector = ResolveNode(env, sexp.Expression);
            if (selector is FluentNone)
            {
                return DefaultMember(env, sexp.Variants, sexp.DefaultIndex);
            }

            // Match the selector against keys of each variant, in order.
            foreach (var variant in sexp.Variants)
            {
                var key = ResolveNode(env, variant.Key);
                bool keyCanMatch =
                  key is FluentNumber || key is FluentSymbol;

                if (!keyCanMatch)
                {
                    continue;
                }

                if (((IFluentType)key).Match(env.Context, selector))
                {
                    return variant.Value;
                }
            }

            return DefaultMember(env, sexp.Variants, sexp.DefaultIndex);
        }

        /**
         * Resolve a reference to an external argument.
         *
         * @param   {Object} env
         *    Resolver environment object.
         * @param   {Object} expr
         *    An expression to be resolved.
         * @param   {String} expr.name
         *    Name of an argument to be returned.
         * @returns {FluentType}
         * @private
         */
        static IFluentType ExternalArgument(ResolverEnvironment env, ExternalArgument externalArg)
        {
            object arg;
            if (env.Arguments == null ||
                !env.Arguments.TryGetValue(externalArg.Name, out arg))
            {
                env.Errors.Add(new ReferenceError($"Unknown external: ${externalArg.Name}"));
                return new FluentNone(externalArg.Name);
            }

            // Return early if the argument already is an instance of IFluentType.
            if (arg is IFluentType ft)
            {
                return ft;
            }

            // Convert the argument to a Fluent type.
            if (arg is string str)
            {
                return new FluentString(str);
            }
            if (arg is sbyte || arg is short || arg is int || arg is long ||
                arg is byte || arg is ushort || arg is uint || arg is ulong ||
                arg is float || arg is double || arg is decimal)
            {
                return new FluentNumber(arg.ToString());
            }
            if (arg is DateTime dt)
            {
                return new FluentDateTime(dt);
            }

            env.Errors.Add(new TypeError(
                $"Unsupported external type: {externalArg.Name}, {arg.GetType()}"));
            return new FluentNone(externalArg.Name);
        }

        /**
         * Resolve a call to a Function with positional and key-value arguments.
         *
         * @param   {Object} env
         *    Resolver environment object.
         * @param   {Object} expr
         *    An expression to be resolved.
         * @param   {Object} expr.fun
         *    FTL Function object.
         * @param   {Array} expr.args
         *    FTL Function argument list.
         * @returns {FluentType}
         * @private
         */
        static IFluentType CallExpression(ResolverEnvironment env, CallExpression expr)
        {
            // Some functions are built-in.  Others may be provided by the runtime via
            // the `MessageContext` constructor.
            ExternalFunction fn;
            if (!env.Context.Functions.TryGetValue(expr.Function, out fn) &&
                !BuiltInFunctions.TryGetValue(expr.Function, out fn))
            {
                env.Errors.Add(new ReferenceError(
                    $"Unknown function: {expr.Function}()"));
                return new FluentNone($"{expr.Function}()");
            }

            var posArgs = new List<object>();
            var keyArgs = new Dictionary<string, object>();

            foreach (var arg in expr.Args)
            {
                if (arg is NamedArgument narg)
                {
                    keyArgs[narg.Name] = ResolveNode(env, narg.Value);
                }
                else
                {
                    posArgs.Add(ResolveNode(env, arg));
                }
            }
            return fn(posArgs, keyArgs);
            // try {
            //     return callee(posargs, keyargs);
            // } catch (e) {
            //     // XXX Report errors.
            //     return new FluentNone();
            // }
        }

        /**
         * Resolve a pattern (a complex string with placeables).
         *
         * @param   {Object} env
         *    Resolver environment object.
         * @param   {Array} ptn
         *    Array of pattern elements.
         * @returns {Array}
         * @private
         */
        static IFluentType Pattern(ResolverEnvironment env, Pattern pattern)
        {
            if (env.Dirty.Contains(pattern))
            {
                env.Errors.Add(new RangeError("Cyclic reference"));
                return new FluentNone();
            }

            // Tag the pattern as dirty for the purpose of the current resolution.
            env.Dirty.Add(pattern);
            var result = new StringBuilder();
            // const result = [];

            // Wrap interpolations with Directional Isolate Formatting characters
            // only when the pattern has more than one element.
            var useIsolating = env.Context.UseIsolating &&
                pattern.Elements.Count > 1;

            foreach (var elem in pattern.Elements)
            {
                if (elem is StringExpression sexp)
                {
                    result.Append(env.Context.Transform(sexp.Value));
                    continue;
                }

                // var part = ((IFluentType)ResolveNode(env, elem)).Format(env.Context);
                var resolved = ((IFluentType)ResolveNode(env, elem));
                var part = resolved.Format(env.Context);

                if (useIsolating)
                {
                    result.Append(FSI);
                }

                if (part.Length > MAX_PLACEABLE_LENGTH)
                {
                    env.Errors.Add(
                      new RangeError(
                        "Too many characters in placeable " +
                        $"({part.Length}, max allowed is {MAX_PLACEABLE_LENGTH})"));
                    result.Append(part.Substring(0, MAX_PLACEABLE_LENGTH));
                }
                else
                {
                    result.Append(part);
                }

                if (useIsolating)
                {
                    result.Append(PDI);
                }
            }

            env.Dirty.Remove(pattern);
            return new FluentString(result.ToString());
        }

        /**
         * Format a translation into a string.
         *
         * @param   {MessageContext} ctx
         *    A MessageContext instance which will be used to resolve the
         *    contextual information of the message.
         * @param   {Object}         args
         *    List of arguments provided by the developer which can be accessed
         *    from the message.
         * @param   {Object}         message
         *    An object with the Message to be resolved.
         * @param   {Array}          errors
         *    An error array that any encountered errors will be appended to.
         * @returns {IFluentType}
         */
        static public string Resolve(MessageContext ctx, Node message,
            IDictionary<string, object> args, ICollection<FluentError> errors)
        {
            var env = new ResolverEnvironment()
            {
                Arguments = args,
                Context = ctx,
                Errors = errors
            };
            return ((IFluentType)ResolveNode(env, message)).Format(ctx);
        }
    }

    /// <summary>
    /// Message contexts are single-language stores of translations.  They are
    /// responsible for parsing translation resources in the Fluent syntax and can
    /// format translation units (entities) to strings.
    /// 
    /// Always use `MessageContext.format` to retrieve translation units from
    /// a context.  Translations can contain references to other entities or
    /// external arguments, conditional logic in form of select expressions, traits
    /// which describe their grammatical features, and can use Fluent builtins which
    /// make use of the `Intl` formatters to format numbers, dates, lists and more
    /// into the context's language.  See the documentation of the Fluent syntax for
    /// more information.
    /// </summary>
    public class MessageContext
    {
        readonly static IDictionary<string, Resolver.ExternalFunction> s_emptyFunctions = new
            Dictionary<string, Resolver.ExternalFunction>();
        public IEnumerable<string> Locales { get; private set; }
        internal IDictionary<string, Message> _messages = new Dictionary<string, Message>();
        internal IDictionary<string, Message> _terms = new Dictionary<string, Message>();
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
        )
        {
            Locales = locales;
            Culture = new CultureInfo(Locales.First());
            if (options != null)
            {
                UseIsolating = options.UseIsolating;
            }
            Transform = options?.Transform ?? NoOpTransform;
            Functions = options?.Functions ?? s_emptyFunctions;
        }

        public MessageContext(
            string locale,
            MessageContextOptions options
        ) : this(new string[] { locale }, options)
        {
        }



        /// <summary>
        /// Return an iterator over public `[id, message]` pairs.
        ///</summary>
        /// @returns {Iterator}
        ///
        IEnumerator<KeyValuePair<string, Message>> Messages
        {
            get { return _messages.GetEnumerator(); }
        }

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
            var parser = new RuntimeParser();
            var resource = parser.GetResource(source);
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
                        resource.Errors.Add(new OverrideError(
                            $"Attempt to override an existing term: \"{entry.Key}\""));
                        continue;
                    }
                    _terms.Add(entry);
                }
                else
                {
                    if (_messages.ContainsKey(entry.Key))
                    {
                        resource.Errors.Add(new OverrideError(
                            $"Attempt to override an existing message: \"{entry.Key}\""));
                        continue;
                    }
                    _messages.Add(entry);
                }
            }
            return resource.Errors;
        }

        public IList<ParseException> AddMessages(string source)
        {
            using (var sr = new StringReader(source))
            {
                return AddMessages(sr);
            }
        }

        /// <summary>
        /// Format a message to a string or null.
        /// 
        /// Format a raw `message` from the context into a string (or a null if it has
        /// a null value).  `args` will be used to resolve references to external
        /// arguments inside of the translation.
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
        ///     [<ReferenceError: Unknown external: name>]
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

      /*
      _memoizeIntlObject(ctor, opts) {
        const cache = _intls.get(ctor) || {};
        const id = JSON.stringify(opts);

        if (!cache[id]) {
          cache[id] = new ctor(locales, opts);
          _intls.set(ctor, cache);
        }

        return cache[id];
      }
    }
    */
    }
}
