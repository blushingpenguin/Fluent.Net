using FluentAssertions;
using JsonDiffPatchDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Fluent.Net.Test
{
    public class SelectExpressionsTest
    {
        static string Ftl(string input) => Util.Ftl(input);

        private MessageContext CreateContext(string ftl)
        {
            var ctx = new MessageContext("en-US", new MessageContextOptions()
                { UseIsolating = false }
            );
            ctx.AddMessages(ftl);
            return ctx;
        }


        [Test]
        public void SelectsTheVariantMatchingTheSelector()
        {
            var ctx = CreateContext(Ftl(@"
                foo = { ""a"" ->
                    [a] A
                   *[b] B
                }
            "));
            var msg = ctx.GetMessage("foo");
            var errors = new List<FluentError>();
            var val = ctx.Format(msg, null, errors);
            val.Should().Be("A");
            errors.Count.Should().Be(0);
        }

        [Test]
        public void SelectsTheDefaultVariantWhenNotMatchingTheSelector()
        {
            var ctx = CreateContext(Ftl(@"
                foo = { ""c"" ->
                   *[a] A
                    [b] B
                }
            "));

            var msg = ctx.GetMessage("foo");
            var errors = new List<FluentError>();
            var val = ctx.Format(msg, null, errors);
            val.Should().Be("A");
            errors.Count.Should().Be(0);
        }

        [Test]
        public void MissingSelectorsSelectsTheDefaultVariantWithError()
        {
            var ctx = CreateContext(Ftl(@"
                foo = { $none ->
                   *[a] A
                    [b] B
                }
            "));
            var msg = ctx.GetMessage("foo");
            var errors = new List<FluentError>();
            var val = ctx.Format(msg, null, errors);
            val.Should().Be("A");
            errors.Count.Should().Be(1);
            errors[0].Should().BeOfType<ReferenceError>();
        }

        MessageContext CreateNumericSelectorContext()
        {
            return CreateContext(Ftl(@"
                foo = { 1 ->
                   *[0] A
                    [1] B
                }

                bar = { 2 ->
                   *[0] A
                    [1] B
                }
            "));
        }

        [Test]
        public void NumericSelectorSelectsTheCorrectVariant()
        {
            var ctx = CreateNumericSelectorContext();
            var msg = ctx.GetMessage("foo");
            var errors = new List<FluentError>();
            var val = ctx.Format(msg, null, errors);
            val.Should().Be("B");
            errors.Count.Should().Be(0);
        }

        [Test]
        public void NumericSelectorSelectsTheDefaultVariant()
        {
            var ctx = CreateNumericSelectorContext();
            var msg = ctx.GetMessage("bar");
            var errors = new List<FluentError>();
            var val = ctx.Format(msg, null, errors);
            val.Should().Be("A");
            errors.Count.Should().Be(0);
        }

        MessageContext CreateNumericAndPluralSelectorContext()
        {
            return CreateContext(Ftl(@"
                foo = { 1 ->
                   *[one] A
                    [other] B
                }

                bar = { 1 ->
                   *[1] A
                    [other] B
                }
            "));
        }

        [Test]
        public void NumericAndPluralSelectorSelectsTheCorrectCategory()
        {
            var ctx = CreateNumericAndPluralSelectorContext();
            var msg = ctx.GetMessage("foo");
            var errors = new List<FluentError>();
            var val = ctx.Format(msg, null, errors);
            val.Should().Be("A");
            errors.Count.Should().Be(0);
        }

        [Test]
        public void NumericAndPluralSelectorSelectsTheExactMatch()
        {
            var ctx = CreateNumericAndPluralSelectorContext();
            var msg = ctx.GetMessage("bar");
            var errors = new List<FluentError>();
            var val = ctx.Format(msg, null, errors);
            val.Should().Be("A");
            errors.Count.Should().Be(0);
        }
    }
}
