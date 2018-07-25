using Fluent.Net.RuntimeAst;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fluent.Net
{
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
}
