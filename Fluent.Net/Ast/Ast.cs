using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fluent.Net.Ast
{
    /*
     * Base class for all Fluent AST nodes.
     *
     * All productions described in the ASDL subclass BaseNode, including Span and
     * Annotation.
     *
     */
    public abstract class BaseNode
    {
        // BaseNode() { }
        public virtual JObject ToJson()
        {
            return new JObject();
        }
    }

    /*
     * Base class for AST nodes which can have Spans.
     */
    public abstract class SyntaxNode : BaseNode
    {
        public Span Span { get; set; }

        internal void AddSpan(int start, int end)
        {
            Span = new Span(start, end);
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["span"] = Span.ToJson();
            return obj;
        }
    }

    public class Resource : SyntaxNode
    {
        public IReadOnlyList<Entry> Body { get; set; }

        public Resource()
        {
        }

        public Resource(IReadOnlyList<Entry> body)
        {
            Body = body;
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["type"] = "Resource";
            obj["body"] = new JArray(Body.Select(x => x.ToJson()));
            return obj;
        }
    }

    public abstract class Entry : SyntaxNode
    {
        readonly List<Annotation> _annotations = new List<Annotation>();
        public IReadOnlyList<Annotation> Annotations { get { return _annotations; } }

        public void AddAnnotation(Annotation annotation)
        {
            _annotations.Add(annotation);
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["annotations"] = new JArray(Annotations.Select(x => x.ToJson()));
            return obj;
        }
    }

    public class Message : Entry
    {
        public Identifier Id { get; set; }
        public Pattern Value { get; set; }
        public IReadOnlyList<Attribute> Attributes { get; set; }
        public BaseComment Comment { get; set; }

        public Message()
        {
        }

        public Message(Identifier id, Pattern value = null,
            IReadOnlyList<Attribute> attributes = null, BaseComment comment = null)
        {
            Id = id;
            Value = value;
            Attributes = attributes;
            Comment = comment;
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["type"] = "Message";
            obj["attributes"] = Attributes == null ? new JArray() : 
                new JArray(Attributes.Select(x => x.ToJson()));
            obj["comment"] = Comment == null ? null : Comment.ToJson();
            obj["id"] = Id.ToJson();
            obj["value"] = Value == null ? null : Value.ToJson();
            return obj;
        }
    }

    public class Term : Entry
    {
        public Identifier Id { get; set; }
        public Pattern Value { get; set; }
        public IReadOnlyList<Attribute> Attributes { get; set; }
        public BaseComment Comment { get; set; }

        public Term()
        {
        }

        public Term(Identifier id, Pattern value,
            IReadOnlyList<Attribute> attributes, BaseComment comment = null)
        {
            Id = id;
            Value = value;
            Attributes = attributes;
            Comment = comment;
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["type"] = "Term";
            obj["attributes"] = Attributes == null ? new JArray() :
                new JArray(Attributes.Select(x => x.ToJson()));
            obj["comment"] = Comment == null ? null : Comment.ToJson();
            obj["id"] = Id.ToJson();
            obj["value"] = Value.ToJson();
            return obj;
        }
    }

    public class Pattern : SyntaxNode
    {
        public IReadOnlyList<SyntaxNode> Elements { get; set; }

        public Pattern()
        {
        }

        public Pattern(IReadOnlyList<SyntaxNode> elements)
        {
            Elements = elements;
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["type"] = "Pattern";
            obj["elements"] = new JArray(Elements.Select(x => x.ToJson()));
            return obj;
        }
    }

    public class TextElement : SyntaxNode
    {
        public string Value { get; set; }

        public TextElement()
        {
        }

        public TextElement(string value)
        {
            Value = value;
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["type"] = "TextElement";
            obj["value"] = Value;
            return obj;
        }
    }

    public class Placeable : SyntaxNode
    {
        public Expression Expression { get; set; }

        public Placeable()
        {
        }

        public Placeable(Expression expression)
        {
            Expression = expression;
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["type"] = "Placeable";
            obj["expression"] = Expression.ToJson();
            return obj;
        }
    }

    public abstract class Expression : SyntaxNode
    {
    }

    public class StringExpression : Expression
    {
        public string Value { get; set; }

        public StringExpression()
        {
        }

        public StringExpression(string value)
        {
            Value = value;
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["type"] = "StringExpression";
            obj["value"] = Value;
            return obj;
        }
    }

    public class NumberExpression : Expression
    {
        public string Value { get; set; }

        public NumberExpression()
        {
        }

        public NumberExpression(string value)
        {
            Value = value;
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["type"] = "NumberExpression";
            obj["value"] = Value;
            return obj;
        }
    }

    public class MessageReference : Expression
    {
        public Identifier Id { get; set; }

        public MessageReference()
        {
        }

        public MessageReference(Identifier id)
        {
            Id = id;
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["type"] = "MessageReference";
            obj["id"] = Id.ToJson();
            return obj;
        }
    }

    public class ExternalArgument : Expression
    {
        public Identifier Id { get; set; }

        public ExternalArgument()
        {
        }

        public ExternalArgument(Identifier id)
        {
            Id = id;
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["type"] = "ExternalArgument";
            obj["id"] = Id.ToJson();
            return obj;
        }
    }

    public class SelectExpression : Expression
    {
        public Expression Expression { get; set; }
        public IReadOnlyList<Variant> Variants { get; set; }

        public SelectExpression()
        {
        }

        public SelectExpression(Expression expression, IReadOnlyList<Variant> variants)
        {
            Expression = expression;
            Variants = variants;
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["type"] = "SelectExpression";
            obj["expression"] = Expression == null ? null : Expression.ToJson();
            obj["variants"] = Variants == null ? new JArray() :
                new JArray(Variants.Select(x => x.ToJson()));
            return obj;
        }
    }

    public class AttributeExpression : Expression
    {
        public Identifier Id { get; set; }
        public Identifier Name { get; set; }

        public AttributeExpression()
        {
        }

        public AttributeExpression(Identifier id, Identifier name)
        {
            Id = id;
            Name = name;
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["type"] = "AttributeExpression";
            obj["id"] = Id.ToJson();
            obj["name"] = Name.ToJson();
            return obj;
        }
    }

    public class VariantExpression : Expression
    {
        public Expression Reference { get; set; }
        public SyntaxNode Key { get; set; }

        public VariantExpression()
        {
        }

        public VariantExpression(Expression reference, SyntaxNode key)
        {
            Reference = reference;
            Key = key;
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["type"] = "VariantExpression";
            obj["ref"] = Reference.ToJson();
            obj["key"] = Key.ToJson();
            return obj;
        }
    }

    public class CallExpression : Expression
    {
        public Function Callee { get; set; }
        public IReadOnlyList<SyntaxNode> Args { get; set; }

        public CallExpression()
        {
        }

        public CallExpression(Function callee, IReadOnlyList<SyntaxNode> args)
        {
            Callee = callee;
            Args = args;
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["type"] = "CallExpression";
            obj["callee"] = Callee.ToJson();
            obj["args"] = Args == null ? new JArray() :
                new JArray(Args.Select(x => x.ToJson()));
            return obj;
        }
    }

    public class Attribute : SyntaxNode
    {
        public Identifier Id { get; set; }
        public Pattern Value { get; set; }

        public Attribute()
        {
        }

        public Attribute(Identifier id, Pattern value)
        {
            Id = id;
            Value = value;
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["type"] = "Attribute";
            obj["id"] = Id.ToJson();
            obj["value"] = Value.ToJson();
            return obj;
        }
    }

    public class Variant : SyntaxNode
    {
        public SyntaxNode Key { get; set; }
        public Pattern Value { get; set; }
        public bool IsDefault { get; set; }

        public Variant()
        {
        }

        public Variant(SyntaxNode key, Pattern value, bool isDefault = false)
        {
            Key = key;
            Value = value;
            IsDefault = isDefault;
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["type"] = "Variant";
            obj["default"] = IsDefault;
            obj["key"] = Key.ToJson();
            obj["value"] = Value.ToJson();
            return obj;
        }
    }

    public class NamedArgument : SyntaxNode
    {
        public Identifier Name { get; set; }
        public Expression Value { get; set; }

        public NamedArgument()
        {
        }

        public NamedArgument(Identifier name, Expression value)
        {
            Name = name;
            Value = value;
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["type"] = "NamedArgument";
            obj["name"] = Name.ToJson();
            obj["value"] = Value.ToJson();
            return obj;
        }
    }

    public class Identifier : SyntaxNode
    {
        public string Name { get; set; }

        public Identifier()
        {
        }

        public Identifier(string name)
        {
            Name = name;
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["type"] = "Identifier";
            obj["name"] = Name;
            return obj;
        }
    }

    public class VariantName : Identifier
    {
        public VariantName()
        {
        }

        public VariantName(string name) :
            base(name)
        {
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["type"] = "VariantName";
            return obj;
        }
    }

    public abstract class BaseComment : Entry
    {
        public string Content { get; set; }

        public BaseComment()
        {
        }

        public BaseComment(string content)
        {
            Content = content;
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["content"] = Content;
            return obj;
        }
    }

    public class Comment : BaseComment
    {
        public Comment()
        {
        }

        public Comment(string content) :
            base(content)
        {
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["type"] = "Comment";
            return obj;
        }
    }

    public class GroupComment : BaseComment
    {
        public GroupComment()
        {
        }

        public GroupComment(string content) : 
            base(content)
        {
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["type"] = "GroupComment";
            return obj;
        }
    }

    public class ResourceComment : BaseComment
    {
        public ResourceComment()
        {
        }

        public ResourceComment(string content) :
            base(content)
        {
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["type"] = "ResourceComment";
            return obj;
        }
    }

    public class Function : Identifier
    {
        public Function()
        {
        }

        public Function(string name) :
            base(name)
        {
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["type"] = "Function";
            return obj;
        }
    }

    public class Junk : Entry
    {
        public string Content { get; set; }

        public Junk()
        {
        }

        public Junk(string content)
        {
            Content = content;
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["type"] = "Junk";
            obj["content"] = Content;
            return obj;
        }
    }

    public class Span : BaseNode
    {
        public int Start { get; set; }
        public int End { get; set; }

        public Span()
        {
        }

        public Span(int start, int end)
        {
            Start = start;
            End = end;
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["type"] = "Span";
            obj["start"] = Start;
            obj["end"] = End;
            return obj;
        }
    }

    public class Annotation : SyntaxNode
    {
        public string Code { get; set; }
        public string[] Args { get; set; }
        public string Message { get; set; }

        public Annotation()
        {
        }

        public Annotation(string code,
            string[] args = null, string message = null)
        {
            Code = code;
            Args = args;
            Message = message;
        }

        public override JObject ToJson()
        {
            var obj = base.ToJson();
            obj["type"] = "Annotation";
            obj["args"] = Args == null ? new JArray() : new JArray(Args);
            obj["code"] = Code;
            obj["message"] = Message;
            return obj;
        }
    }
}
