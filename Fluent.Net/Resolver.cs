using Fluent.Net.RuntimeAst;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fluent.Net
{
/**
 * @overview
 *
 * The role of the Fluent resolver is to format a translation object to an
 * instance of `FluentType` or an array of instances.
 *
 * Translations can contain references to other messages or variables,
 * conditional logic in form of select expressions, traits which describe their
 * grammatical features, and can use Fluent builtins which make use of the
 * `Intl` formatters to format numbers, dates, lists and more into the
 * context's language. See the documentation of the Fluent syntax for more
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
            if (expr is StringLiteral se)
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
            if (expr is NumberLiteral num)
            {
                return new FluentNumber(num.Value);
            }
            if (expr is VariableReference arg)
            {
                return VariableReference(env, arg);
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
            if (expr is GetAttribute attrExpr)
            {
                var attr = AttributeExpression(env, attrExpr);
                return ResolveNode(env, attr);
            }
            if (expr is GetVariant varExpr)
            {
                var variant = VariantExpression(env, varExpr);
                return ResolveNode(env, variant);
            }
            if (expr is SelectExpression sel)
            {
                var member = SelectExpression(env, sel);
                return ResolveNode(env, member);
            }
            env.Errors?.Add(new RangeError("No value"));
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

            env.Errors?.Add(new RangeError("No default"));
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
            (isTerm ? env.Context._terms
                    : env.Context._messages).TryGetValue(ref_.Name, out Message message);

            if (message == null)
            {
                var err = isTerm
                    ? new ReferenceError($"Unknown term: {ref_.Name}")
                    : new ReferenceError($"Unknown message: {ref_.Name}");
                env.Errors?.Add(err);
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
        static Node VariantExpression(ResolverEnvironment env, GetVariant expr)
        {
            var message = MessageReference(env, expr.Id);
            if (message is FluentNone)
            {
                return message;
            }
            if (message is Message actualMessage)
            {
                message = actualMessage.Value;
                if (message is Pattern pattern)
                {
                    if (pattern.Elements.Any())
                    {
                        message = pattern.Elements.First();
                    }
                }
            }

            var keyword = (IFluentType)ResolveNode(env, expr.Key);

            if (message is SelectExpression sexp)
            {
                // Match the specified key against keys of each variant, in order.
                foreach (var variant in sexp.Variants)
                {
                    var key = ResolveNode(env, variant.Key);
                    if (keyword.Match(env.Context, key))
                    {
                        return variant.Value;
                    }
                }
            }

            env.Errors?.Add(new ReferenceError(
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
        static Node AttributeExpression(ResolverEnvironment env, GetAttribute expr)
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
                if (messageNode.Attributes.TryGetValue(expr.Name, out Node value))
                {
                    return value;
                }
            }

            env.Errors?.Add(new ReferenceError($"Unknown attribute: {expr.Name}"));
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
         * Resolve a reference to a variable
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
        static IFluentType VariableReference(ResolverEnvironment env, VariableReference varReference)
        {
            if (env.Arguments == null ||
                !env.Arguments.TryGetValue(varReference.Name, out object arg))
            {
                env.Errors?.Add(new ReferenceError($"Unknown variable: ${varReference.Name}"));
                return new FluentNone(varReference.Name);
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

            env.Errors?.Add(new TypeError(
                $"Unsupported variable type: {varReference.Name}, {arg?.GetType()?.ToString() ?? "null"}"));
            return new FluentNone(varReference.Name);
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
            if (!env.Context.Functions.TryGetValue(expr.Function, out ExternalFunction fn) &&
                !BuiltInFunctions.TryGetValue(expr.Function, out fn))
            {
                env.Errors?.Add(new ReferenceError(
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
                env.Errors?.Add(new RangeError("Cyclic reference"));
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
                if (elem is StringLiteral sexp)
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
                    env.Errors?.Add(
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
}
