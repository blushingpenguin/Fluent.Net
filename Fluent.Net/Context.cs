using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Fluent.Net
{
    public class MessageContextOptions
    {
        public bool UseIsolating { get; set; }
    }

    public static class Resolver
    {
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
        /* function Type(env, expr)
        {
            // A fast-path for strings which are the most common case, and for
            // `FluentNone` which doesn't require any additional logic.
            if (typeof expr === "string")
            {
                return env.ctx._transform(expr);
            }
            if (expr instanceof FluentNone) {
                return expr;
            }

            // The Runtime AST (Entries) encodes patterns (complex strings with
            // placeables) as Arrays.
            if (Array.isArray(expr))
            {
                return Pattern(env, expr);
            }


            switch (expr.type)
            {
                case "varname":
                    return new FluentSymbol(expr.name);
                case "num":
                    return new FluentNumber(expr.val);
                case "ext":
                    return ExternalArgument(env, expr);
                case "fun":
                    return FunctionReference(env, expr);
                case "call":
                    return CallExpression(env, expr);
                case "ref":
                    {
                        const message = MessageReference(env, expr);
                        return Type(env, message);
                    }
                case "attr":
                    {
                        const attr = AttributeExpression(env, expr);
                        return Type(env, attr);
                    }
                case "var":
                    {
                        const variant = VariantExpression(env, expr);
                        return Type(env, variant);
                    }
                case "sel":
                    {
                        const member = SelectExpression(env, expr);
                        return Type(env, member);
                    }
                case undefined:
                    {
                        // If it's a node with a value, resolve the value.
                        if (expr.val !== null && expr.val !== undefined)
                        {
                            return Type(env, expr.val);
                        }

                        const { errors } = env;
                        errors.push(new RangeError("No value"));
                        return new FluentNone();
                    }
                default:
                    return new FluentNone();
            }
        }*/

#if FALSE
/**
 * @overview
 *
 * The role of the Fluent resolver is to format a translation object to an
 * instance of `FluentType` or an array of instances.
 *
 * Translations can contain references to other messages or external arguments,
 * conditional logic in form of select expressions, traits which describe their
 * grammatical features, and can use Fluent builtins which make use of the
 * `Intl` formatters to format numbers, dates, lists and more into the
 * context's language.  See the documentation of the Fluent syntax for more
 * information.
 *
 * In case of errors the resolver will try to salvage as much of the
 * translation as possible.  In rare situations where the resolver didn't know
 * how to recover from an error it will return an instance of `FluentNone`.
 *
 * `MessageReference`, `VariantExpression`, `AttributeExpression` and
 * `SelectExpression` resolve to raw Runtime Entries objects and the result of
 * the resolution needs to be passed into `Type` to get their real value.
 * This is useful for composing expressions.  Consider:
 *
 *     brand-name[nominative]
 *
 * which is a `VariantExpression` with properties `id: MessageReference` and
 * `key: Keyword`.  If `MessageReference` was resolved eagerly, it would
 * instantly resolve to the value of the `brand-name` message.  Instead, we
 * want to get the message object and look for its `nominative` variant.
 *
 * All other expressions (except for `FunctionReference` which is only used in
 * `CallExpression`) resolve to an instance of `FluentType`.  The caller should
 * use the `toString` method to convert the instance to a native value.
 *
 *
 * All functions in this file pass around a special object called `env`.
 * This object stores a set of elements used by all resolve functions:
 *
 *  * {MessageContext} ctx
 *      context for which the given resolution is happening
 *  * {Object} args
 *      list of developer provided arguments that can be used
 *  * {Array} errors
 *      list of errors collected while resolving
 *  * {WeakSet} dirty
 *      Set of patterns already encountered during this resolution.
 *      This is used to prevent cyclic resolutions.
 */


import { FluentType, FluentNone, FluentNumber, FluentDateTime, FluentSymbol }
  from "./types";
import builtins from "./builtins";

// Prevent expansion of too long placeables.
const MAX_PLACEABLE_LENGTH = 2500;

// Unicode bidi isolation characters.
const FSI = "\u2068";
const PDI = "\u2069";


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
function DefaultMember(env, members, def) {
  if (members[def]) {
    return members[def];
  }

  const { errors } = env;
  errors.push(new RangeError("No default"));
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
function MessageReference(env, {name}) {
  const { ctx, errors } = env;
  const message = name.startsWith("-")
    ? ctx._terms.get(name)
    : ctx._messages.get(name);

  if (!message) {
    const err = name.startsWith("-")
      ? new ReferenceError(`Unknown term: ${name}`)
      : new ReferenceError(`Unknown message: ${name}`);
    errors.push(err);
    return new FluentNone(name);
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
function VariantExpression(env, {id, key}) {
  const message = MessageReference(env, id);
  if (message instanceof FluentNone) {
    return message;
  }

  const { ctx, errors } = env;
  const keyword = Type(env, key);

  function isVariantList(node) {
    return Array.isArray(node) &&
      node[0].type === "sel" &&
      node[0].exp === null;
  }

  if (isVariantList(message.val)) {
    // Match the specified key against keys of each variant, in order.
    for (const variant of message.val[0].vars) {
      const variantKey = Type(env, variant.key);
      if (keyword.match(ctx, variantKey)) {
        return variant;
      }
    }
  }

  errors.push(new ReferenceError(`Unknown variant: ${keyword.toString(ctx)}`));
  return Type(env, message);
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
function AttributeExpression(env, {id, name}) {
  const message = MessageReference(env, id);
  if (message instanceof FluentNone) {
    return message;
  }

  if (message.attrs) {
    // Match the specified name against keys of each attribute.
    for (const attrName in message.attrs) {
      if (name === attrName) {
        return message.attrs[name];
      }
    }
  }

  const { errors } = env;
  errors.push(new ReferenceError(`Unknown attribute: ${name}`));
  return Type(env, message);
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
function SelectExpression(env, {exp, vars, def}) {
  if (exp === null) {
    return DefaultMember(env, vars, def);
  }

  const selector = Type(env, exp);
  if (selector instanceof FluentNone) {
    return DefaultMember(env, vars, def);
  }

  // Match the selector against keys of each variant, in order.
  for (const variant of vars) {
    const key = Type(env, variant.key);
    const keyCanMatch =
      key instanceof FluentNumber || key instanceof FluentSymbol;

    if (!keyCanMatch) {
      continue;
    }

    const { ctx } = env;

    if (key.match(ctx, selector)) {
      return variant;
    }
  }

  return DefaultMember(env, vars, def);
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
function ExternalArgument(env, {name}) {
  const { args, errors } = env;

  if (!args || !args.hasOwnProperty(name)) {
    errors.push(new ReferenceError(`Unknown external: ${name}`));
    return new FluentNone(name);
  }

  const arg = args[name];

  // Return early if the argument already is an instance of FluentType.
  if (arg instanceof FluentType) {
    return arg;
  }

  // Convert the argument to a Fluent type.
  switch (typeof arg) {
    case "string":
      return arg;
    case "number":
      return new FluentNumber(arg);
    case "object":
      if (arg instanceof Date) {
        return new FluentDateTime(arg);
      }
    default:
      errors.push(
        new TypeError(`Unsupported external type: ${name}, ${typeof arg}`)
      );
      return new FluentNone(name);
  }
}

/**
 * Resolve a reference to a function.
 *
 * @param   {Object}  env
 *    Resolver environment object.
 * @param   {Object} expr
 *    An expression to be resolved.
 * @param   {String} expr.name
 *    Name of the function to be returned.
 * @returns {Function}
 * @private
 */
function FunctionReference(env, {name}) {
  // Some functions are built-in.  Others may be provided by the runtime via
  // the `MessageContext` constructor.
  const { ctx: { _functions }, errors } = env;
  const func = _functions[name] || builtins[name];

  if (!func) {
    errors.push(new ReferenceError(`Unknown function: ${name}()`));
    return new FluentNone(`${name}()`);
  }

  if (typeof func !== "function") {
    errors.push(new TypeError(`Function ${name}() is not callable`));
    return new FluentNone(`${name}()`);
  }

  return func;
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
function CallExpression(env, {fun, args}) {
  const callee = FunctionReference(env, fun);

  if (callee instanceof FluentNone) {
    return callee;
  }

  const posargs = [];
  const keyargs = {};

  for (const arg of args) {
    if (arg.type === "narg") {
      keyargs[arg.name] = Type(env, arg.val);
    } else {
      posargs.push(Type(env, arg));
    }
  }

  try {
    return callee(posargs, keyargs);
  } catch (e) {
    // XXX Report errors.
    return new FluentNone();
  }
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
function Pattern(env, ptn) {
  const { ctx, dirty, errors } = env;

  if (dirty.has(ptn)) {
    errors.push(new RangeError("Cyclic reference"));
    return new FluentNone();
  }

  // Tag the pattern as dirty for the purpose of the current resolution.
  dirty.add(ptn);
  const result = [];

  // Wrap interpolations with Directional Isolate Formatting characters
  // only when the pattern has more than one element.
  const useIsolating = ctx._useIsolating && ptn.length > 1;

  for (const elem of ptn) {
    if (typeof elem === "string") {
      result.push(ctx._transform(elem));
      continue;
    }

    const part = Type(env, elem).toString(ctx);

    if (useIsolating) {
      result.push(FSI);
    }

    if (part.length > MAX_PLACEABLE_LENGTH) {
      errors.push(
        new RangeError(
          "Too many characters in placeable " +
          `(${part.length}, max allowed is ${MAX_PLACEABLE_LENGTH})`
        )
      );
      result.push(part.slice(MAX_PLACEABLE_LENGTH));
    } else {
      result.push(part);
    }

    if (useIsolating) {
      result.push(PDI);
    }
  }

  dirty.delete(ptn);
  return result.join("");
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
 * @returns {FluentType}
 */
export default function resolve(ctx, args, message, errors = []) {
  const env = {
    ctx, args, errors, dirty: new WeakSet()
  };
  return Type(env, message).toString(ctx);
}
#endif
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
        string[] _locales;
        IDictionary<string, object> _messages = new Dictionary<string, object>();
        IDictionary<string, object> _terms = new Dictionary<string, object>();

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
            MessageContextOptions   options /* = {
          functions = {},
          useIsolating = true,
          transform = v => v
        } = {}*/)
        {
            _locales = locales.ToArray();
  
            /*_terms = new Map();
            _messages = new Map();
            _functions = functions;
            _useIsolating = useIsolating;
            _transform = transform;
            _intls = new WeakMap();*/
        }
        //public MessageContext(
        //  string  locale)
        //{


        /// <summary>
        /// Return an iterator over public `[id, message]` pairs.
        ///</summary>
        /// @returns {Iterator}
        ///
        IEnumerator<KeyValuePair<string, object>> Messages
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
        public object GetMessage(string id)
        {
            object value;
            _messages.TryGetValue(id, out value);
            return value;
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
        public IList<string> AddMessages(TextReader source)
        {
            var errors = new List<string>();
            var parser = new Parser(false);
            var resource = parser.Parse(source);
            foreach (var entry in resource.Body)
            {
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
                    _terms.Add(t.Id.Name, entry);
                }
                else if (entry is Ast.Message m)
                {
                    if (_messages.ContainsKey(m.Id.Name))
                    {
                        errors.Add($"Attempt to override an existing message: \"{m.Id.Name}\"");
                        continue;
                    }
                    _messages.Add(m.Id.Name, entry);
                }
                // else Ast.Comment or Ast.Junk
            }
            return errors;
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
        string format(object message, IDictionary<string, string> args = null, ICollection<string> errors = null)
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
            return "todo";
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
