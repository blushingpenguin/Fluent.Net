using FluentAssertions;
using NUnit.Framework;
using System;
using System.IO;

namespace Fluent.Net.Test
{
    public class SerializerExpressionTest : SerializerTestBase
    {
        static string Pretty(string text)
        {
            using (var sr = new StringReader(text))
            {
                var entry = new Parser().ParseEntry(sr);
                entry.Should().BeAssignableTo<Ast.MessageTermBase>();
                var message = (Ast.MessageTermBase)entry;
                message.Value.Should().BeOfType(typeof(Ast.Pattern));
                var pattern = (Ast.Pattern)message.Value;
                pattern.Elements.Count.Should().BeGreaterOrEqualTo(1);
                pattern.Elements[0].Should().BeOfType(typeof(Ast.Placeable));
                var placeable = (Ast.Placeable)pattern.Elements[0];
                using (var sw = new StringWriter())
                {
                    new Serializer().SerializeExpression(sw, placeable.Expression);
                    return sw.ToString();
                }
            }
        }

        [Test]
        public void InvalidArguments()
        {
            var serializer = new Serializer();
            Action a = () =>
            {
                using (var sw = new StringWriter())
                {
                    serializer.SerializeExpression(sw, null);
                }
            };
            a.Should().Throw<ArgumentNullException>();

            a = () => serializer.SerializeExpression(null, new Ast.StringExpression());
            a.Should().Throw<ArgumentNullException>();

            a = () => serializer.SerializeExpression(null, null);
            a.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void StringExpression()
        {
            var input = Ftl(@"
              foo = { ""str"" }
            ");
            Pretty(input).Should().Be("\"str\"");
        }

        [Test]
        public void NumberExpression()
        {
            var input = Ftl(@"
              foo = { 3 }
            ");
            Pretty(input).Should().Be("3");
        }

        [Test]
        public void MessageReference()
        {
            var input = Ftl(@"
              foo = { msg }
            ");
            Pretty(input).Should().Be("msg");
        }

        [Test]
        public void ExternalArgument()
        {
            var input = Ftl(@"
              foo = { $ext }
            ");
            Pretty(input).Should().Be("$ext");
        }

        [Test]
        public void AttributeExpression()
        {
            var input = Ftl(@"
              foo = { msg.attr }
            ");
            Pretty(input).Should().Be("msg.attr");
        }

        [Test]
        public void VariantExpression()
        {
            var input = Ftl(@"
              foo = { -msg[variant] }
            ");
            Pretty(input).Should().Be("-msg[variant]");
        }

        [Test]
        public void CallExpression()
        {
            var input = Ftl(@"
              foo = { BUILTIN(3.14, kwarg: ""value"") }
            ");
            Pretty(input).Should().Be(@"BUILTIN(3.14, kwarg: ""value"")");
        }

        [Test]
        public void SelectExpression()
        {

            var input = Ftl(@"
              foo =
                  { $num ->
                      *[one] One
                  }
            ");
            Pretty(input).Should().Be("$num ->\n   *[one] One\n");
        }
    }
}
