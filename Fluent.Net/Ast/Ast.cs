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

        internal void AddSpan(Position start, Position end)
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

    /// <summary>
    /// An abstract base class for useful elements of Resource.body.
    /// </summary>
    public abstract class Entry : SyntaxNode
    {
    }

    public abstract class MessageTermBase : Entry
    {
        public Identifier Id { get; set; }
        public SyntaxNode Value { get; set; }
        public IReadOnlyList<Attribute> Attributes { get; set; }
        public Comment Comment { get; set; }

        protected MessageTermBase()
        {
        }

        protected MessageTermBase(Identifier id, SyntaxNode value = null,
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

        public Message(Identifier id, Pattern pattern = null,
            IReadOnlyList<Attribute> attributes = null, Comment comment = null) :
            base(id, pattern, attributes, comment)
        {
        }
    }

    public class Term : MessageTermBase
    {
        public Term()
        {
        }

        public Term(Identifier id, SyntaxNode value,
            IReadOnlyList<Attribute> attributes, Comment comment = null) :
            base(id, value, attributes, comment)
        {
            Value = value;
        }
    }

    public class VariantList : SyntaxNode
    {
        public VariantList()
        {
        }

        public IReadOnlyList<Variant> Variants { get; set; }

        public VariantList(IReadOnlyList<Variant> variants)
        {
            Variants = variants;
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

    public abstract class PatternElement : SyntaxNode
    {
    }

    public class TextElement : PatternElement
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

    public class Placeable : PatternElement
    {
        public SyntaxNode Expression { get; set; }

        public Placeable()
        {
        }

        public Placeable(SyntaxNode expression)
        {
            Expression = expression;
        }
    }

    public abstract class Expression : SyntaxNode
    {
    }

    public class StringLiteral : Expression
    {
        public string Value { get; set; }

        public StringLiteral()
        {
        }

        public StringLiteral(string value)
        {
            Value = value;
        }
    }

    public class NumberLiteral : Expression
    {
        public string Value { get; set; }

        public NumberLiteral()
        {
        }

        public NumberLiteral(string value)
        {
            Value = value;
        }
    }

    public abstract class MessageTermReference : Expression
    {
        public Identifier Id { get; set; }

        protected MessageTermReference()
        {
        }

        protected MessageTermReference(Identifier id)
        {
            Id = id;
        }
    }

    public class MessageReference : MessageTermReference
    {
        public MessageReference()
        {
        }

        public MessageReference(Identifier id) :
            base(id)
        {
        }
    }

    public class TermReference : MessageTermReference
    {
        public TermReference()
        {
        }

        public TermReference(Identifier id) :
            base(id)
        {
        }
    }

    public class VariableReference : Expression
    {
        public Identifier Id { get; set; }

        public VariableReference()
        {
        }

        public VariableReference(Identifier id)
        {
            Id = id;
        }
    }

    public class SelectExpression : Expression
    {
        public SyntaxNode Selector { get; set; }
        public IReadOnlyList<Variant> Variants { get; set; }

        public SelectExpression()
        {
        }

        public SelectExpression(SyntaxNode selector, IReadOnlyList<Variant> variants)
        {
            Selector = selector;
            Variants = variants;
        }
    }

    public class AttributeExpression : Expression
    {
        public MessageTermReference Ref { get; set; }
        public Identifier Name { get; set; }

        public AttributeExpression()
        {
        }

        public AttributeExpression(MessageTermReference ref_, Identifier name)
        {
            Ref = ref_;
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
        public IReadOnlyList<SyntaxNode> Positional { get; set; }
        public IReadOnlyList<NamedArgument> Named { get; set; }

        public CallExpression()
        {
        }

        public CallExpression(Function callee, IReadOnlyList<SyntaxNode> positional,
            IReadOnlyList<NamedArgument> named)
        {
            Callee = callee;
            Positional = positional;
            Named = named;
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
        public SyntaxNode Value { get; set; }
        public bool IsDefault { get; set; }

        public Variant()
        {
        }

        public Variant(SyntaxNode key, SyntaxNode value, bool isDefault = false)
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
        readonly List<Annotation> _annotations = new List<Annotation>();
        public IReadOnlyList<Annotation> Annotations { get { return _annotations; } }

        public Junk()
        {
        }

        public Junk(string content)
        {
            Content = content;
        }

        public void AddAnnotation(Annotation annotation)
        {
            _annotations.Add(annotation);
        }
    }

    public class Span : BaseNode
    {
        public Position Start { get; set; }
        public Position End { get; set; }

        public Span()
        {
        }

        public Span(Position start, Position end)
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
