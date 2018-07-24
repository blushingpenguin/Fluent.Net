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
    public class MessageContextTest
    {
        static string Ftl(string input) => Util.Ftl(input);

        private MessageContext CreateContext(string ftl)
        {
            var ctx = new MessageContext("en-US", new MessageContextOptions()
                { UseIsolating =  false });
            ctx.AddMessages(ftl);
            return ctx;
        }

        private MessageContext CreateAddMessagesContext()
        {
            return CreateContext(Ftl(@"
                foo = Foo
                -bar = Private Bar
            "));
        }

        [Test]
        public void AddMessage_AddsMessages()
        {
            var ctx = CreateAddMessagesContext();
            ctx._messages.Should().ContainKey("foo");
            ctx._terms.Should().NotContainKey("foo");
            ctx._messages.Should().NotContainKey("-bar");
            ctx._terms.Should().ContainKey("-bar");
        }

        [Test]
        public void AddMessage_PreservesExistingMessagesWhenNewAreAdded()
        {
            var ctx = CreateAddMessagesContext();
            ctx.AddMessages(Ftl(@"
                baz = Baz
            "));

            ctx._messages.Should().ContainKey("foo");
            ctx._terms.Should().NotContainKey("foo");
            ctx._messages.Should().NotContainKey("-bar");
            ctx._terms.Should().ContainKey("-bar");

            ctx._messages.Should().ContainKey("baz");
            ctx._terms.Should().NotContainKey("baz");
        }


        [Test]
        public void AddMessage_MessageAndTermNamesCanBeTheSame()
        {
            var ctx = CreateAddMessagesContext();
            ctx.AddMessages(Ftl(@"
                -foo = Private Foo
            "));

            ctx._messages.Should().ContainKey("foo");
            ctx._terms.Should().NotContainKey("foo");
            ctx._messages.Should().NotContainKey("-foo");
            ctx._terms.Should().ContainKey("-foo");
        }

        [Test]
        public void AddMessage_MessagesWithTheSameIdAreNotOverwritten()
        {
            var ctx = CreateAddMessagesContext();
            var errors = ctx.AddMessages(Ftl(@"
                foo = New Foo
            "));

            // Attempt to overwrite error reported
            errors.Count.Should().Be(1);
            ctx._messages.Count.Should().Be(1);

            var msg = ctx.GetMessage("foo");
            errors = new List<string>();
            var formatErrors = new List<FluentError>();
            var val = ctx.Format(msg, null, formatErrors);
            val.Should().Be("Foo");
            formatErrors.Count.Should().Be(0);
        }

        private MessageContext CreateHasMessageContext()
        {
            return CreateContext(Ftl(@"
                foo = Foo
                -bar = Bar
            "));
        }


        [Test]
        public void HasMessage_OnlyReturnsTrueForPublicMessages()
        {
            var ctx = CreateHasMessageContext();
            ctx.HasMessage("foo").Should().BeTrue();
        }

        [Test]
        public void HasMessage_ReturnsFalseForTermsAndMissingMessages()
        {
            var ctx = CreateHasMessageContext();
            ctx.HasMessage("-bar").Should().BeFalse();
            ctx.HasMessage("-baz").Should().BeFalse();
            ctx.HasMessage("-baz").Should().BeFalse();
        }

        [Test]
        public void GetMessage_ReturnsPublicMessages()
        {
            var ctx = CreateHasMessageContext();
            var expected = new RuntimeAst.Message()
            {
                Value = new RuntimeAst.StringExpression() { Value = "Foo" }
            };
            ctx.GetMessage("foo").Should().BeEquivalentTo(expected);
        }

        [Test]
        public void GetMessageReturnsNullForTermsAndMissingMessages()
        {
            var ctx = CreateHasMessageContext();
            ctx.GetMessage("-bar").Should().BeNull();
            ctx.GetMessage("-baz").Should().BeNull();
            ctx.GetMessage("-baz").Should().BeNull();
        }
    }
}
