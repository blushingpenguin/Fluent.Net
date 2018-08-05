using FluentAssertions;
using NUnit.Framework;
using System;
using System.IO;

namespace Fluent.Net.Test
{
    public class SerializerTest
    {
        static string Ftl(string input) => Util.Ftl(input);

        [Test]
        public void SimpleAst()
        {
            var input = new Ast.Resource()
            {
                Span = new Ast.Span(0, 9),
                Body = new Ast.Entry[]
                {
                    new Ast.Message()
                    {
                        Comment = null,
                        Span = new Ast.Span(0, 9),
                        Attributes = new Ast.Attribute[0],
                        Id = new Ast.Identifier()
                        {
                            Name = "foo",
                            Span = new Ast.Span(0, 3)
                        },
                        Value = new Ast.Pattern()
                        {
                            Elements = new Ast.SyntaxNode[]
                            {
                                new Ast.TextElement("Foo")
                            },
                            Span = new Ast.Span(6, 9)
                        }
                    }
                }
            };

            var serializer = new Serializer();
            using (var sw = new StringWriter())
            {
                serializer.Serialize(sw, input);
                string actual = sw.ToString();
                string expected = "foo = Foo\n";
                actual.Should().Be(expected);
            }
        }

        static string Pretty(string text)
        {
            using (var sr = new StringReader(text))
            using (var sw = new StringWriter())
            {
                var parser = new Parser();
                var resource = parser.Parse(sr);
                var serializer = new Serializer();
                serializer.Serialize(sw, resource);
                return sw.ToString();
            }
        }

        [Test]
        public void InvalidResource()
        {
            var serializer = new Serializer();

            Action a = () =>
            {
                using (var sw = new StringWriter())
                {
                    serializer.Serialize(sw, null);
                }
            };
            a.Should().Throw<ArgumentNullException>();

            a = () => serializer.Serialize(null, new Ast.Resource());
            a.Should().Throw<ArgumentNullException>();

            a = () => serializer.Serialize(null, null);
            a.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void SimpleMessage()
        {
            var input = Ftl(@"
                foo = Foo
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void SimpleTerm()
        {
            var input = Ftl(@"
                -foo = Foo
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void TwoSimpleMessages()
        {
            var input = Ftl(@"
              foo = Foo
              bar = Bar
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void BlockMultilineMessage()
        {
            var input = Ftl(@"
              foo =
                  Foo
                  Bar
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void InlineMultilineMessage()
        {
            var input = Ftl(@"
              foo = Foo
                  Bar
            ");
            var output = Ftl(@"
              foo =
                  Foo
                  Bar
            ");
            Pretty(input).Should().Be(output);
        }

        [Test]
        public void MessageReference()
        {
            var input = Ftl(@"
              foo = Foo { bar }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void TermReference()
        {
            var input = Ftl(@"
              foo = Foo { -bar }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void Variable()
        {
            var input = Ftl(@"
              foo = Foo { $bar }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void NumberElement()
        {
            var input = Ftl(@"
              foo = Foo { 1 }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void StringElement()
        {
            var input = Ftl(@"
              foo = Foo { ""bar"" }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void VariantExpression()
        {
            var input = Ftl(@"
              foo = Foo { -bar[baz] }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void AttributeExpression()
        {
            var input = Ftl(@"
              foo = Foo { bar.baz }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void ResourceComment()
        {
            var input = Ftl(@"
              ### A multiline
              ### resource comment.

              foo = Foo
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void MessageComment()
        {
            var input = Ftl(@"
              # A multiline
              # message comment.
              foo = Foo
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void GroupComment()
        {
            var input = Ftl(@"
              foo = Foo

              ## Comment Header
              ##
              ## A multiline
              ## group comment.

              bar = Bar
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void StandaloneComment()
        {
            var input = Ftl(@"
              foo = Foo

              # A Standalone Comment

              bar = Bar
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void MultilineWithPlaceable()
        {
            var input = Ftl(@"
              foo =
                  Foo { bar }
                  Baz
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void Attribute()
        {
            var input = Ftl(@"
              foo =
                  .attr = Foo Attr
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void MultilineAttribute()
        {
            var input = Ftl(@"
              foo =
                  .attr =
                      Foo Attr
                      Continued
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void TwoAttributes()
        {
            var input = Ftl(@"
              foo =
                  .attr-a = Foo Attr A
                  .attr-b = Foo Attr B
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void ValueAndAttributes()
        {
            var input = Ftl(@"
              foo = Foo Value
                  .attr-a = Foo Attr A
                  .attr-b = Foo Attr B
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void MultilineValueAndAttributes()
        {
            var input = Ftl(@"
              foo =
                  Foo Value
                  Continued
                  .attr-a = Foo Attr A
                  .attr-b = Foo Attr B
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void VariantList()
        {
            var input = Ftl(@"
              -foo =
                  {
                     *[a] A
                      [b] B
                  }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void SelectExpression()
        {
            var input = Ftl(@"
              foo =
                  { $sel ->
                     *[a] A
                      [b] B
                  }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void MultilineVariant()
        {
            var input = Ftl(@"
              foo =
                  { $sel ->
                     *[a]
                          AAA
                          BBB
                  }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void MultilineVariantWithFirstLineInline()
        {
            var input = Ftl(@"
              foo =
                  { $sel ->
                     *[a] AAA
                          BBB
                  }
            ");
            var output = Ftl(@"
              foo =
                  { $sel ->
                     *[a]
                          AAA
                          BBB
                  }
            ");
            Pretty(input).Should().Be(output);
        }

        [Test]
        public void VariantKeyWords()
        {
            var input = Ftl(@"
              foo =
                  { $sel ->
                     *[a b c] A B C
                  }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void VariantKeyNumber()
        {
            var input = Ftl(@"
              foo =
                  { $sel ->
                     *[1] 1
                  }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void SelectExpresionInBlockValue()
        {
            var input = Ftl(@"
              foo =
                  Foo { $sel ->
                     *[a] A
                      [b] B
                  }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void SelectExpresionInInlineValue()
        {
            var input = Ftl(@"
              foo = Foo { $sel ->
                     *[a] A
                      [b] B
                  }
            ");
            var output = Ftl(@"
              foo =
                  Foo { $sel ->
                     *[a] A
                      [b] B
                  }
            ");
            Pretty(input).Should().Be(output);
        }

        [Test]
        public void SelectExpresionInMultilineValue()
        {
            var input = Ftl(@"
              foo =
                  Foo
                  Bar { $sel ->
                     *[a] A
                      [b] B
                  }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void NestedSelectExpression()
        {
            var input = Ftl(@"
              foo =
                  { $a ->
                     *[a]
                          { $b ->
                             *[b] Foo
                          }
                  }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void SelectorVariable()
        {
            var input = Ftl(@"
              foo =
                  { $bar ->
                     *[a] A
                  }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void SelectorNumberExpression()
        {
            var input = Ftl(@"
              foo =
                  { 1 ->
                     *[a] A
                  }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void SelectorStringExpression()
        {
            var input = Ftl(@"
              foo =
                  { ""bar"" ->
                     *[a] A
                  }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void SelectorAttributeExpression()
        {
            var input = Ftl(@"
              foo =
                  { -bar.baz ->
                     *[a] A
                  }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void CallExpression()
        {
            var input = Ftl(@"
              foo = { FOO() }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void CallExpressionWithStringExpression()
        {
            var input = Ftl(@"
              foo = { FOO(""bar"") }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void CallExpressionWithNumberExpression()
        {
            var input = Ftl(@"
              foo = { FOO(1) }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void CallExpressionWithMessageReference()
        {
            var input = Ftl(@"
              foo = { FOO(bar) }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void CallExpressionWithVariable()
        {
            var input = Ftl(@"
              foo = { FOO($bar) }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void CallExpressionWithNumberNamedArgument()
        {
            var input = Ftl(@"
              foo = { FOO(bar: 1) }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void CallExpressionWithStringNamedArgument()
        {
            var input = Ftl(@"
              foo = { FOO(bar: ""bar"") }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void CallExpressionWithTwoPositionalArguments()
        {
            var input = Ftl(@"
              foo = { FOO(bar, baz) }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void CallExpressionWithTwoNamedArguments()
        {
            var input = Ftl(@"
              foo = { FOO(bar: ""bar"", baz: ""baz"") }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void CallExpressionWithPositionalAndNamedArguments()
        {
            var input = Ftl(@"
              foo = { FOO(bar, 1, baz: ""baz"") }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void NestedPlaceables()
        {
            var input = Ftl(@"
                foo = {{ FOO() }}
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void EscapedSpecialCharInTextElement()
        {
            var input = Ftl(@"
                foo = \{Escaped}
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void EscapedSpecialCharInStringLiteral()
        {
            var input = Ftl(@"
                foo = { ""Escaped \"" quote"" }
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void UnicodeEscapeSequence()
        {
            var input = Ftl(@"
                foo = \u0065
            ");
            Pretty(input).Should().Be(input);
        }

        static string PrettyExpr(string text)
        {
            using (var sr = new StringReader(text))
            using (var sw = new StringWriter())
            {
                var parser = new Parser();
                var entry = parser.ParseEntry(sr);
                entry.Should().BeOfType<Ast.Message>();
                var message = (Ast.Message)entry;
                message.Value.Should().BeOfType<Ast.Pattern>();
                var pattern = (Ast.Pattern)message.Value;
                pattern.Elements.Count.Should().BeGreaterThan(0);
                pattern.Elements[0].Should().BeOfType<Ast.Placeable>();
                var placeable = (Ast.Placeable)pattern.Elements[0];
                var serializer = new Serializer();
                serializer.SerializeExpression(sw, placeable.Expression);
                return sw.ToString();
            }
        }

        [Test]
        public void ExprStringExpression()
        {
            var input = Ftl(@"
                foo = { ""str"" }
            ");
            PrettyExpr(input).Should().Be("\"str\"");
        }

        [Test]
        public void NumberExpression()
        {
            var input = Ftl(@"
                foo = { 3 }
            ");
            PrettyExpr(input).Should().Be("3");
        }

        [Test]
        public void ExprMessageReference()
        {
            var input = Ftl(@"
                foo = { msg }
            ");
            PrettyExpr(input).Should().Be("msg");
        }

        [Test]
        public void ExprVariable()
        {
            var input = Ftl(@"
                foo = { $ext }
            ");
            PrettyExpr(input).Should().Be("$ext");
        }

        [Test]
        public void ExprAttributeExpression()
        {
            var input = Ftl(@"
                foo = { msg.attr }
            ");
            PrettyExpr(input).Should().Be("msg.attr");
        }

        [Test]
        public void ExprVariantExpression()
        {
            var input = Ftl(@"
                foo = { -msg[variant] }
            ");
            PrettyExpr(input).Should().Be("-msg[variant]");
        }

        [Test]
        public void ExprCallExpression()
        {
            var input = Ftl(@"
                foo = { BUILTIN(3.14, kwarg: ""value"") }
            ");
            var output = "BUILTIN(3.14, kwarg: \"value\")";
            PrettyExpr(input).Should().Be(output);
        }

        [Test]
        public void ExprSelectExpression()
        {
            var input = Ftl(@"
                foo =
                        { $num ->
                                *[one] One
                        }
            ");
            PrettyExpr(input).Should().Be("$num ->\n   *[one] One\n");
        }

        [Test]
        public void StandaloneCommentHasNotPaddingWhenFirst()
        {
            var input = Ftl(@"
                # Comment A

                foo = Foo

                # Comment B

                bar = Bar
            ");
            Pretty(input).Should().Be(input);
            // Run again to make sure the same instance of the serializer doesn't keep
            // state about how many entires is has already serialized.
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void GroupCommentHasnotPaddingWhenFirst()
        {
            var input = Ftl(@"
                ## Group A

                foo = Foo

                ## Group B

                bar = Bar
            ");
            Pretty(input).Should().Be(input);
        }

        [Test]
        public void ResourceCommentHasnotPaddingWhenFirst()
        {
            var input = Ftl(@"
                ### Resource Comment A

                foo = Foo

                ### Resource Comment B

                bar = Bar
            ");
            Pretty(input).Should().Be(input);
        }
    }
}
