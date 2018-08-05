using Fluent.Net.RuntimeAst;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fluent.Net.Test
{
    static class RuntimeAstToJson
    {
        public static JToken ToJson(Node node)
        {
            if (node == null)
            {
                return JValue.CreateNull();
            }
            return ToJson((dynamic)node);
        }

        public static JToken ToJson(VariableReference arg)
        {
            return new JObject
            {
                { "type", "var" },
                { "name", arg.Name }
            };
        }

        public static JToken ToJson(StringLiteral str)
        {
            return new JValue(str.Value);
        }

        public static JToken ToJson(NumberLiteral num)
        {
            return new JObject
            {
                { "type", "num" },
                { "val", num.Value }
            };
        }

        public static JToken ToJson(Pattern pattern)
        {
            return ToJson(pattern.Elements);
        }

        public static JToken ToJson(VariantName name)
        {
            return new JObject
            {
                { "type", "varname" },
                { "name", name.Name }
            };
        }

        static JToken ToJson(Variant var)
        {
            return new JObject
            {
                { "key", ToJson(var.Key) },
                { "val", ToJson(var.Value) }
            };
        }

        public static JToken ToJson(GetVariant var)
        {
            return new JObject
            {
                { "type", "getvar" },
                { "id", ToJson(var.Id) },
                { "key", ToJson(var.Key) }
            };
        }

        public static JToken ToJson(int? val)
        {
            return val.HasValue ? new JValue(val.Value) : JValue.CreateNull();
        }

        public static JToken ToJson<T>(IEnumerable<T> val) where T : Node
        {
            return new JArray(val.Select(x => ToJson(x)));
        }

        public static JToken ToJson(SelectExpression sel)
        {
            return new JObject
            {
                { "type", "sel" },
                { "exp", ToJson(sel.Expression) },
                { "vars", new JArray(sel.Variants.Select(x => ToJson(x))) },
                { "def", ToJson(sel.DefaultIndex) }
            };
        }

        public static JToken ToJson(GetAttribute attr)
        {
            return new JObject
            {
                { "type", "getattr" },
                { "id", ToJson(attr.Id) },
                { "name", attr.Name }
            };
        }

        public static JToken ToJson(MessageReference reference)
        {
            return new JObject
            {
                { "type", "ref" },
                { "name", reference.Name }
            };
        }

        public static JToken ToJson(CallExpression call)
        {
            return new JObject
            {
                { "type", "call" },
                { "fun", new JObject
                    {
                        { "type", "fun" },
                        { "name", call.Function }
                    }
                },
                { "args", ToJson(call.Args) }
            };
        }

        public static JToken ToJson(NamedArgument arg)
        {
            return new JObject
            {
                { "type", "narg" },
                { "name", arg.Name },
                { "val", ToJson(arg.Value) }
            };
        }

        public static JToken ToJson<T>(IDictionary<string, T> dic)
        {
            var result = new JObject();
            foreach (var entry in dic)
            {
                result.Add(entry.Key, ToJson((dynamic)entry.Value));
            }
            return result;
        }

        public static JToken ToJson(Message message)
        {
            JToken val = message.Value == null ? null : ToJson(message.Value);
            if (message.Attributes == null &&
                val is JValue jv && jv.Type == JTokenType.String)
            {
                return val;
            }
            var result = new JObject();
            if (val != null)
            {
                result["val"] = val;
            }
            if (message.Attributes != null)
            {
                JObject attrs = new JObject();
                foreach (var attribute in message.Attributes)
                {
                    // The javascript version produces "att-name": "att-value" for simple 
                    // string values, and "att-name": { val: [ "one", "two" ] } for 'complex'
                    // patterns.  The object wrapping the array ({val:[]}) is unnecessary,
                    // but we need to produce it here for the tests to get the expected
                    // outputs.
                    JToken attributeValue;
                    if (attribute.Value is StringLiteral se)
                    {
                        attributeValue = new JValue(se.Value);
                    }
                    else if (attribute.Value is Pattern p)
                    {
                        var valueObject = new JObject();
                        valueObject.Add("val", ToJson(p.Elements));
                        attributeValue = valueObject;
                    }
                    else
                    {
                        throw new Exception(
                            $"Unknown attribute value type {attribute.Value.GetType()}");
                    }
                    attrs.Add(attribute.Key, attributeValue);
                }
                result["attrs"] = attrs;
            }
            return result;
        }
    }
}
