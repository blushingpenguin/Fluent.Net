using System.Collections.Generic;

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
    }

    public abstract class Entry : SyntaxNode
    {
        readonly List<Annotation> _annotations = new List<Annotation>();
        public IReadOnlyList<Annotation> Annotations { get { return _annotations; } }

        public void AddAnnotation(Annotation annotation)
        {
            _annotations.Add(annotation);
        }
    }

    public abstract class MessageTermBase : Entry
    {
        public Identifier Id { get; set; }
        public Pattern Value { get; set; }
        public IReadOnlyList<Attribute> Attributes { get; set; }
        public Comment Comment { get; set; }

        protected MessageTermBase()
        {
        }

        protected MessageTermBase(Identifier id, Pattern value = null,
            IReadOnlyList<Attribute> attributes = null, Comment comment = null)
        {
            Id = id;
            Value = value;
            Attributes = attributes;
            Comment = comment;
        }
    }

    public class Message : MessageTermBase
    {
        public Message()
        {
        }

        public Message(Identifier id, Pattern value = null,
            IReadOnlyList<Attribute> attributes = null, Comment comment = null) :
            base(id, value, attributes, comment)
        {
        }
    }

    public class Term : MessageTermBase
    {
        public Term()
        {
        }

        public Term(Identifier id, Pattern value,
            IReadOnlyList<Attribute> attributes, Comment comment = null) :
            base(id, value, attributes, comment)
        {
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
    }
}
