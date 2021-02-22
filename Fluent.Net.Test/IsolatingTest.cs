using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Fluent.Net.Test
{
    public class IsolatingTest : MessageContextTestBase
    {
        // Unicode bidi isolation characters.
        const char FSI = '\u2068';
        const char PDI = '\u2069';

        MessageContext CreateInterpolationsContext()
        {
            return CreateContext(@"
                foo = Foo
                bar = { foo } Bar
                baz = { $arg } Baz
                qux = { bar } { baz }
            ", true);
        }

        [Test]
        public void IsolatesInterpolatedMessageReferences()
        {
            var ctx = CreateInterpolationsContext();
            var msg = ctx.GetMessage("bar");
            var errors = new List<FluentError>();
            var val = ctx.Format(msg, null, errors);
            val.Should().Be($"{FSI}Foo{PDI} Bar");
            errors.Count.Should().Be(0);
        }

        [Test]
        public void IsolatesInterpolatedStringTypedVariables()
        {
            var ctx = CreateInterpolationsContext();
            var errors = new List<FluentError>();
            var args = new Dictionary<string, object> { { "arg", "Arg" } };
            var msg = ctx.GetMessage("baz");
            var val = ctx.Format(msg, args, errors);
            val.Should().Be($"{FSI}Arg{PDI} Baz");
            errors.Count.Should().Be(0);
        }

        [Test]
        public void IsolatesInterpolatedNumberTypedVariables()
        {
            var ctx = CreateInterpolationsContext();
            var errors = new List<FluentError>();
            var args = new Dictionary<string, object> { { "arg", 1 } };
            var msg = ctx.GetMessage("baz");
            var val = ctx.Format(msg, args, errors);
            val.Should().Be($"{FSI}1{PDI} Baz");
            errors.Count.Should().Be(0);
        }

        [Test]
        public void IsolatesInterpolatedDateTypedVariables()
        {
            var ctx = CreateInterpolationsContext();
            var errors = new List<FluentError>();
            var dt = new DateTime(2016, 09, 29);
            var args = new Dictionary<string, object> { { "arg", dt } };
            var msg = ctx.GetMessage("baz");
            var val = ctx.Format(msg, args, errors);
            var dtf = dt.ToString(ctx.Culture);
            val.Should().Be($"{FSI}{dtf}{PDI} Baz");
            errors.Count.Should().Be(0);
        }

        [Test]
        public void IsolatesComplexInterpolations()
        {
            var ctx = CreateInterpolationsContext();
            var errors = new List<FluentError>();
            var args = new Dictionary<string, object> { { "arg", "Arg" } };
            var msg = ctx.GetMessage("qux");
            var val = ctx.Format(msg, args, errors);
            errors.Count.Should().Be(0);
            var expected_bar = $"{FSI}{FSI}Foo{PDI} Bar{PDI}";
            var expected_baz = $"{FSI}{FSI}Arg{PDI} Baz{PDI}";
            val.Should().Be($"{expected_bar} {expected_baz}");
            errors.Count.Should().Be(0);
        }

        MessageContext CreateSkipIsolationContext()
        {
            return CreateContext(@"
                -brand-short-name = Amaya
                foo = { -brand-short-name }
            ", true);
        }

        [Test]
        public void SkipsIsolationIfTheOnlyElementIsAPlaceable()
        {
            var ctx = CreateSkipIsolationContext();
            var errors = new List<FluentError>();
            var msg = ctx.GetMessage("foo");
            var val = ctx.Format(msg, null, errors);
            val.Should().Be("Amaya");
            errors.Count.Should().Be(0);
        }
    }
}
