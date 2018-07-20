using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fluent.Net.Ast;

namespace Fluent.Net.Test
{
    static class AstToJson
    {
        public static JToken ToJson(BaseNode node)
        {
            if (node == null)
            {
                return JValue.CreateNull();
            }
            return ToJson((dynamic)node);
        }

        public static JToken ToJson(SyntaxNode node)
        {
            var obj = new JObject();
            obj["span"] = ToJson(node.Span);
            return obj;
        }

        public static JToken ToJson(Resource resource)
        {
            var obj = (JObject)ToJson((SyntaxNode)resource);
            obj["type"] = "Resource";
            obj["body"] = new JArray(resource.Body.Select(
                x => ToJson((dynamic)x)));
            return obj;
        }

        public static JToken ToJson(Entry entry)
        {
            var obj = (JObject)ToJson((SyntaxNode)entry);
            obj["annotations"] = new JArray(entry.Annotations.Select(
                x => ToJson(x)));
            return obj;
        }

        public static JToken ToJson(MessageTermBase mt)
        {
            var obj = (JObject)ToJson((Entry)mt);
            obj["attributes"] = mt.Attributes == null ? new JArray() :
                new JArray(mt.Attributes.Select(x => ToJson(x)));
            obj["comment"] = mt.Comment == null ? null : ToJson(mt.Comment);
            obj["id"] = mt.Id == null ? null : ToJson((dynamic)mt.Id);
            obj["value"] = mt.Value == null ? null : ToJson(mt.Value);
            return obj;
        }

        public static JToken ToJson(Message m)
        {
            var obj = (JObject)ToJson((MessageTermBase)m);
            obj["type"] = "Message";
            return obj;
        }

        public static JToken ToJson(Term t)
        {
            var obj = (JObject)ToJson((MessageTermBase)t);
            obj["type"] = "Term";
            return obj;
        }

        public static JToken ToJson(Pattern p)
        {
            var obj = (JObject)ToJson((SyntaxNode)p);
            obj["type"] = "Pattern";
            obj["elements"] = new JArray(p.Elements.Select(x =>
                ToJson((dynamic)x)));
            return obj;
        }

        public static JToken ToJson(TextElement te)
        {
            var obj = (JObject)ToJson((SyntaxNode)te);
            obj["type"] = "TextElement";
            obj["value"] = te.Value;
            return obj;
        }

        public static JToken ToJson(Placeable p)
        {
            var obj = (JObject)ToJson((SyntaxNode)p);
            obj["type"] = "Placeable";
            obj["expression"] = ToJson((dynamic)p.Expression);
            return obj;
        }

        public static JToken ToJson(StringExpression se)
        {
            var obj = (JObject)ToJson((Expression)se);
            obj["type"] = "StringExpression";
            obj["value"] = se.Value;
            return obj;
        }

        public static JToken ToJson(NumberExpression ne)
        {
            var obj = (JObject)ToJson((Expression)ne);
            obj["type"] = "NumberExpression";
            obj["value"] = ne.Value;
            return obj;
        }

        public static JToken ToJson(MessageReference me)
        {
            var obj = (JObject)ToJson((Expression)me);
            obj["type"] = "MessageReference";
            obj["id"] = ToJson((dynamic)me.Id);
            return obj;
        }

        public static JToken ToJson(ExternalArgument arg)
        {
            var obj = (JObject)ToJson((Expression)arg);
            obj["type"] = "ExternalArgument";
            obj["id"] = ToJson((dynamic)arg.Id);
            return obj;
        }

        public static JToken ToJson(SelectExpression se)
        {
            var obj = (JObject)ToJson((Expression)se);
            obj["type"] = "SelectExpression";
            obj["expression"] = se.Expression == null ? null : 
                ToJson((dynamic)se.Expression);
            obj["variants"] = se.Variants == null ? new JArray() :
                new JArray(se.Variants.Select(x => ToJson(x)));
            return obj;
        }

        public static JToken ToJson(AttributeExpression ae)
        {
            var obj = (JObject)ToJson((Expression)ae);
            obj["type"] = "AttributeExpression";
            obj["id"] = ToJson((dynamic)ae.Id);
            obj["name"] = ToJson((dynamic)ae.Name);
            return obj;
        }

        public static JToken ToJson(VariantExpression ve)
        {
            var obj = (JObject)ToJson((Expression)ve);
            obj["type"] = "VariantExpression";
            obj["ref"] = ToJson((dynamic)ve.Reference);
            obj["key"] = ToJson((dynamic)ve.Key);
            return obj;
        }

        public static JToken ToJson(CallExpression ce)
        {
            var obj = (JObject)ToJson((Expression)ce);
            obj["type"] = "CallExpression";
            obj["callee"] = ToJson(ce.Callee);
            obj["args"] = ce.Args == null ? new JArray() :
                new JArray(ce.Args.Select(x => ToJson((dynamic)x)));
            return obj;
        }

        public static JToken ToJson(Ast.Attribute a)
        {
            var obj = (JObject)ToJson((SyntaxNode)a);
            obj["type"] = "Attribute";
            obj["id"] = ToJson((dynamic)a.Id);
            obj["value"] = ToJson(a.Value);
            return obj;
        }

        public static JToken ToJson(Variant v)
        {
            var obj = (JObject)ToJson((SyntaxNode)v);
            obj["type"] = "Variant";
            obj["default"] = v.IsDefault;
            obj["key"] = ToJson((dynamic)v.Key);
            obj["value"] = ToJson((dynamic)v.Value);
            return obj;
        }

        public static JToken ToJson(NamedArgument arg)
        {
            var obj = (JObject)ToJson((SyntaxNode)arg);
            obj["type"] = "NamedArgument";
            obj["name"] = ToJson(arg.Name);
            obj["value"] = ToJson((dynamic)arg.Value);
            return obj;
        }

        public static JToken ToJson(Identifier id)
        {
            var obj = (JObject)ToJson((SyntaxNode)id);
            obj["type"] = "Identifier";
            obj["name"] = id.Name;
            return obj;
        }

        public static JToken ToJson(VariantName id)
        {
            var obj = (JObject)ToJson((Identifier)id);
            obj["type"] = "VariantName";
            return obj;
        }

        public static JToken ToJson(BaseComment c)
        {
            var obj = (JObject)ToJson((Entry)c);
            obj["content"] = c.Content;
            return obj;
        }

        public static JToken ToJson(Comment c)
        {
            var obj = (JObject)ToJson((BaseComment)c);
            obj["type"] = "Comment";
            return obj;
        }

        public static JToken ToJson(GroupComment c)
        {
            var obj = (JObject)ToJson((BaseComment)c);
            obj["type"] = "GroupComment";
            return obj;
        }

        public static JToken ToJson(ResourceComment c)
        {
            var obj = (JObject)ToJson((BaseComment)c);
            obj["type"] = "ResourceComment";
            return obj;
        }

        public static JToken ToJson(Function fun)
        {
            var obj = (JObject)ToJson((Identifier)fun);
            obj["type"] = "Function";
            return obj;
        }

        public static JToken ToJson(Junk junk)
        {
            var obj = (JObject)ToJson((Entry)junk);
            obj["type"] = "Junk";
            obj["content"] = junk.Content;
            return obj;
        }

        public static JToken ToJson(Span span)
        {
            var obj = new JObject();
            obj["type"] = "Span";
            obj["start"] = span.Start;
            obj["end"] = span.End;
            return obj;
        }

        public static JToken ToJson(Annotation annotation)
        {
            var obj = (JObject)ToJson((SyntaxNode)annotation);
            obj["type"] = "Annotation";
            obj["args"] = annotation.Args == null ? new JArray() :
                new JArray(annotation.Args);
            obj["code"] = annotation.Code;
            obj["message"] = annotation.Message;
            return obj;
        }
    }
}
