using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;

namespace Fluent.Net.Test
{
    public class PrimitivesTest
    {
        static string Ftl(string input) => Util.Ftl(input);

        MessageContext 
        CreateContext(string ftl)
        {
            var locales = new string[] { "en-US", "en" };
            var ctx = new MessageContext(locales, new MessageContextOptions()
                { UseIsolating = false });
            var errors = ctx.AddMessages(ftl);
            errors.Should().BeEquivalentTo(new List<ParseException>());
            return ctx;
        }

        MessageContext CreateNumbersContext()
        {
            return CreateContext(Ftl(@"
                one     = { 1 }
                select  = { 1 ->
                   *[0] Zero
                    [1] One
                }
            "));
        }

        [Test]
        public void NumberCanBeUsedInAPlaceable()
        {
            var ctx = CreateNumbersContext();
            var msg = ctx.GetMessage("one");
            var errors = new List<FluentError>();
            var val = ctx.Format(msg, null, errors);
            val.Should().Be("1");
            errors.Count.Should().Be(0);
        }

        [Test]
        public void NumberCanBeUsedAsASelector()
        {
            var ctx = CreateNumbersContext();
            var msg = ctx.GetMessage("select");
            var errors = new List<FluentError>();
            var val = ctx.Format(msg, null, errors);
            val.Should().Be("One");
            errors.Count.Should().Be(0);
        }

        MessageContext CreateStringValueContext()
        {
            return CreateContext(Ftl(@"
                foo               = Foo

                placeable-literal = { ""Foo"" } Bar
                placeable-message = { foo } Bar

                selector-literal = { ""Foo"" ->
                   *[Foo] Member 1
                }

                bar
                    .attr = Bar Attribute

                placeable-attr   = { bar.attr }

                -baz = Baz
                    .attr = Baz Attribute

                selector-attr    = { -baz.attr ->
                   *[Baz Attribute] Member 3
                }
            "));
        }

        [Test]
        public void CanBeUsedAsAValue()
        {
            var ctx = CreateStringValueContext();
            var msg = ctx.GetMessage("foo");
            var errors = new List<FluentError>();
            var val = ctx.Format(msg, null, errors);
            val.Should().Be("Foo");
            errors.Count.Should().Be(0);
        }

        [Test]
        public void IsDetectedToBeNonComplex()
        {
            var ctx = CreateStringValueContext();
            var msg = ctx.GetMessage("foo");
            msg.Value.Should().BeOfType<RuntimeAst.StringExpression>();
        }

        [Test]
        public void StringCanBeUsedInAPlaceable()
        {
            var ctx = CreateStringValueContext();
            var msg = ctx.GetMessage("placeable-literal");
            var errors = new List<FluentError>();
            var val = ctx.Format(msg, null, errors);
            val.Should().Be("Foo Bar");
            errors.Count.Should().Be(0);
        }

        [Test]
        public void StringCanBeAValueOfAMessageReferencedInAPlaceable()
        {
            var ctx = CreateStringValueContext();
            var msg = ctx.GetMessage("placeable-message");
            var errors = new List<FluentError>();
            var val = ctx.Format(msg, null, errors);
            val.Should().Be("Foo Bar");
            errors.Count.Should().Be(0);
        }

        [Test]
        public void StringCanBeASelector()
        {
            var ctx = CreateStringValueContext();
            var msg = ctx.GetMessage("selector-literal");
            var errors = new List<FluentError>();
            var val = ctx.Format(msg, null, errors);
            val.Should().Be("Member 1");
            errors.Count.Should().Be(0);
        }

        [Test]
        public void StringCanBeUsedAsAnAttributeValue()
        {
            var ctx = CreateStringValueContext();
            var attr = ctx.GetMessage("bar").Attributes["attr"];
            var errors = new List<FluentError>();
            var val = ctx.Format(attr, null, errors);
            val.Should().Be("Bar Attribute");
            errors.Count.Should().Be(0);
        }

        [Test]
        public void StringCanBeAValueOfAnAttributeUsedInAPlaceable()
        {
            var ctx = CreateStringValueContext();
            var msg = ctx.GetMessage("placeable-attr");
            var errors = new List<FluentError>();
            var val = ctx.Format(msg, null, errors);
            val.Should().Be("Bar Attribute");
            errors.Count.Should().Be(0);
        }

        [Test]
        public void StringCanBeAValueOfAnAttributeUsedAsASelector()
        {
            var ctx = CreateStringValueContext();
            var msg = ctx.GetMessage("selector-attr");
            var errors = new List<FluentError>();
            var val = ctx.Format(msg, null, errors);
            val.Should().Be("Member 3");
            errors.Count.Should().Be(0);
        }

        MessageContext CreateComplexStringContext()
        {
            return CreateContext(Ftl(@"
                foo               = Foo
                bar               = { foo } Bar

                placeable-message = { bar } Baz

                baz
                    .attr = { bar } Baz Attribute

                -baz = Baz
                    .attr = Foo Bar Baz Attribute

                placeable-attr = { baz.attr }

                selector-attr = { -baz.attr ->
                    [Foo Bar Baz Attribute] Variant
                   *[ok] Valid
                }
            "));
        }

        [Test]
        public void ComplexStringCanBeUsedAsAValue()
        {
            var ctx = CreateComplexStringContext();
            var msg = ctx.GetMessage("bar");
            var errors = new List<FluentError>();
            var val = ctx.Format(msg, null, errors);
            val.Should().Be("Foo Bar");
            errors.Count.Should().Be(0);
        }

        [Test]
        public void ComplexStringIsDetectedToBeComplex()
        {
            var ctx = CreateComplexStringContext();
            var msg = ctx.GetMessage("bar");
            msg.Value.Should().BeOfType<RuntimeAst.Pattern>();
        }

        [Test]
        public void ComplexStringCanBeAValueOfAMessageReferencedInAPlaceable()
        {
            var ctx = CreateComplexStringContext();
            var msg = ctx.GetMessage("placeable-message");
            var errors = new List<FluentError>();
            var val = ctx.Format(msg, null, errors);
            val.Should().Be("Foo Bar Baz");
            errors.Count.Should().Be(0);
        }

        [Test]
        public void ComplexStringCanBeUsedAsAnAttributeValue()
        {
            var ctx = CreateComplexStringContext();
            var msg = ctx.GetMessage("baz").Attributes["attr"];
            var errors = new List<FluentError>();
            var val = ctx.Format(msg, null, errors);
            val.Should().Be("Foo Bar Baz Attribute");
            errors.Count.Should().Be(0);
        }

        [Test]
        public void ComplexStringCanBeAValueOfAnAttributeUsedInAPlaceable()
        {
            var ctx = CreateComplexStringContext();
            var msg = ctx.GetMessage("placeable-attr");
            var errors = new List<FluentError>();
            var val = ctx.Format(msg, null, errors);
            val.Should().Be("Foo Bar Baz Attribute");
            errors.Count.Should().Be(0);
        }

        [Test]
        public void ComplexStringCanBeAValueOfAnAttributeUsedAsASelector()
        {
            var ctx = CreateComplexStringContext();
            var msg = ctx.GetMessage("selector-attr");
            var errors = new List<FluentError>();
            var val = ctx.Format(msg, null, errors);
            val.Should().Be("Variant");
            errors.Count.Should().Be(0);
        }
    }
}
