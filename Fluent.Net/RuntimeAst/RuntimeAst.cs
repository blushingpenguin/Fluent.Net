using System.Collections.Generic;

namespace Fluent.Net.RuntimeAst
{
    public abstract class Node
    {
    }

    public class VariableReference : Node
    {
        public string Name { get; set; }
    }

    public class StringLiteral : Node
    {
        public string Value { get; set; }
    }

    public class NumberLiteral : Node
    {
        public string Value { get; set; }
    }

    public class Pattern : Node
    {
        public ICollection<Node> Elements { get; set; }
    }

    public class VariantName : Node
    {
        public string Name { get; set; }
    }

    public class Variant : Node
    {
        public Node Key { get; set; }
        public Node Value { get; set; }
    }

    public class GetVariant : Node
    {
        public MessageReference Id { get; set; }
        public Node Key { get; set; }
    }

    public class SelectExpression : Node
    {
        public Node Expression { get; set; }
        public IList<Variant> Variants { get; set; }
        public int? DefaultIndex { get; set; }
    }

    public class GetAttribute : Node
    {
        public MessageReference Id { get; set; }
        public string Name { get; set; }
    }

    public class MessageReference : Node
    {
        public string Name { get; set; }
    }

    public class CallExpression : Node
    {
        public string Function { get; set; }
        public IList<Node> Args { get; set; }
    }

    public class NamedArgument : Node
    {
        public string Name { get; set; }
        public Node Value { get; set; }
    }

    public class Message : Node
    {
        public IDictionary<string, Node> Attributes { get; set; }
        public Node Value { get; set; }
    }
}
