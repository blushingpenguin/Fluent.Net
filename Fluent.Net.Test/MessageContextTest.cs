using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Fluent.Net.Test
{
    public class MessageContextTest : MessageContextTestBase
    {
        private MessageContext CreateAddMessagesContext()
        {
            return CreateContext(@"
                foo = Foo
                -bar = Private Bar
            ");
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
            var formatErrors = new List<FluentError>();
            var val = ctx.Format(msg, null, formatErrors);
            val.Should().Be("Foo");
            formatErrors.Count.Should().Be(0);
        }

        private MessageContext CreateHasMessageContext()
        {
            return CreateContext(@"
                foo = Foo
                -bar = Bar
            ");
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
                Value = new RuntimeAst.StringLiteral() { Value = "Foo" }
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

        [Test]
        public void TestRangeError()
        {
            var error = new RangeError("Test Message");
            error.Message.Should().Be("Test Message");
        }

        [Test]
        public void TestTypeError()
        {
            var error = new TypeError("Test Other Message");
            error.Message.Should().Be("Test Other Message");
        }

        [Test]
        public void TestReferenceError()
        {
            var error = new ReferenceError("Test Reference Message");
            error.Message.Should().Be("Test Reference Message");
        }

        [Test]
        public void TestOverrideError()
        {
            var error = new OverrideError("Test Override Message");
            error.Message.Should().Be("Test Override Message");
        }

        [Test]
        public void TestFluentNone()
        {
            var ctx = new MessageContext("en-US");
            var none = new FluentNone("value");
            none.Value.Should().Be("value");
            none.Format(ctx).Should().Be("value");

            var none2 = new FluentNone();
            none2.Format(ctx).Should().Be("???");
            none2.Value.Should().BeNull();

            none.Match(ctx, none2).Should().BeTrue();
            none.Match(ctx, "no").Should().BeFalse();
        }

        [Test]
        public void TestFluentString()
        {
            var str = new FluentString("test string");
            var ctx = new MessageContext("en-US");
            str.Match(ctx, new FluentString("test string")).Should().BeTrue();
            str.Match(ctx, "fifty").Should().BeFalse();
            str.Match(ctx, "test string").Should().BeTrue();
            str.Match(ctx, 45).Should().BeFalse();
        }

        [Test]
        public void TestFluentNumber()
        {
            var num = new FluentNumber("105.6");
            var ctx = new MessageContext("en-US");
            num.Value.Should().Be("105.6");
            num.Match(ctx, new FluentNumber("105.6")).Should().BeTrue();
            num.Match(ctx, new FluentNumber("105.5")).Should().BeFalse();
            num.Match(ctx, 105.6F).Should().BeTrue();
            num.Match(ctx, 105.6).Should().BeTrue();
            num.Match(ctx, 105.6M).Should().BeTrue();
            num.Match(ctx, (sbyte)105).Should().BeFalse();
            num.Match(ctx, (short)105).Should().BeFalse();
            num.Match(ctx, (int)105).Should().BeFalse();
            num.Match(ctx, (long)105).Should().BeFalse();
            num.Match(ctx, (byte)105).Should().BeFalse();
            num.Match(ctx, (ushort)105).Should().BeFalse();
            num.Match(ctx, (uint)105).Should().BeFalse();
            num.Match(ctx, (ulong)105).Should().BeFalse();
            num.Match(ctx, "whatever").Should().BeFalse();
            num.Format(ctx).Should().Be("105.6");
        }

        [Test]
        public void TestFluentDateTime()
        {
            var dt = new FluentDateTime(new DateTime(2009, 01, 02));
            var ctx = new MessageContext("en-US");
            dt.Format(ctx).Should().Be("1/2/2009 12:00:00 AM");
            dt.Match(ctx, new FluentDateTime(new DateTime(2009, 01, 02))).Should().BeTrue();
            dt.Match(ctx, new FluentDateTime(new DateTime(2009, 01, 03))).Should().BeFalse();
            dt.Match(ctx, new DateTime(2009, 01, 02)).Should().BeTrue();
            dt.Match(ctx, new DateTime(2009, 01, 03)).Should().BeFalse();
            dt.Match(ctx, "not really").Should().BeFalse();

        }

        [Test]
        public void TestFluentSymbol()
        {
            var symbol = new FluentSymbol("sym");
            var ctx = new MessageContext("en-US");
            symbol.Format(ctx).Should().Be("sym");

            symbol.Match(ctx, new FluentSymbol("sym")).Should().BeTrue();
            symbol.Match(ctx, new FluentSymbol("notsym")).Should().BeFalse();
            symbol.Match(ctx, "sym").Should().BeTrue();
            symbol.Match(ctx, "notsym").Should().BeFalse();
            symbol.Match(ctx, new FluentString("sym")).Should().BeTrue();
            symbol.Match(ctx, new FluentString("notsym")).Should().BeFalse();
            symbol.Match(ctx, 442).Should().BeFalse();
        }

        [Test]
        public void TestFluentSymbolUsesLocaleRules()
        {
            var lt = new MessageContext("lt");
            var us = new MessageContext(new string[] { "en-US", "en" });

            var one = new FluentSymbol("one");
            var few = new FluentSymbol("few");
            var many = new FluentSymbol("many");
            var other = new FluentSymbol("other");

            var num1 = new FluentNumber("1");
            var num11 = new FluentNumber("11");
            var num2 = new FluentNumber("2");
            var num0_6 = new FluentNumber("0.6");

            one.Match(lt, num1).Should().BeTrue();
            one.Match(lt, num11).Should().BeFalse();
            one.Match(lt, num2).Should().BeFalse();
            one.Match(lt, num0_6).Should().BeFalse();

            one.Match(us, num1).Should().BeTrue();
            one.Match(us, num11).Should().BeFalse();
            one.Match(us, num2).Should().BeFalse();
            one.Match(us, num0_6).Should().BeFalse();

            few.Match(lt, num1).Should().BeFalse();
            few.Match(lt, num11).Should().BeFalse();
            few.Match(lt, num2).Should().BeTrue();
            few.Match(lt, num0_6).Should().BeFalse();

            few.Match(us, num1).Should().BeFalse();
            few.Match(us, num11).Should().BeFalse();
            few.Match(us, num2).Should().BeFalse();
            few.Match(us, num0_6).Should().BeFalse();

            many.Match(lt, num1).Should().BeFalse();
            many.Match(lt, num11).Should().BeFalse();
            many.Match(lt, num2).Should().BeFalse();
            many.Match(lt, num0_6).Should().BeTrue();

            many.Match(us, num1).Should().BeFalse();
            many.Match(us, num11).Should().BeFalse();
            many.Match(us, num2).Should().BeFalse();
            many.Match(us, num0_6).Should().BeFalse();

            other.Match(lt, num1).Should().BeFalse();
            other.Match(lt, num11).Should().BeTrue();
            other.Match(lt, num2).Should().BeFalse();
            other.Match(lt, num0_6).Should().BeFalse();

            other.Match(lt, num1).Should().BeFalse();
            other.Match(us, num11).Should().BeTrue();
            other.Match(us, num2).Should().BeTrue();
            other.Match(us, num0_6).Should().BeTrue();
        }

        [Test]
        public void VariantTest()
        {
            var context = CreateContext(@"
                -brand-name =
                    {
                       *[nominative] Firefox
                        [accusative] Firefoxa
                    }
                    .gender = masculine

                update-command =
                    Zaktualizuj { -brand-name[accusative] }.

                update-successful =
                    { -brand-name.gender ->
                        [masculine] { -brand-name } został pomyślnie zaktualizowany.
                        [feminine] { -brand-name } została pomyślnie zaktualizowana.
                       *[other] Program { -brand-name } został pomyślnie zaktualizowany.
                    }
            ");
            var errors = new List<FluentError>();
            var msg = context.GetMessage("update-successful");
            var result = context.Format(msg, null, errors);
            result.Should().Be("Firefox został pomyślnie zaktualizowany.");
            errors.Count.Should().Be(0);

            msg = context.GetMessage("update-command");
            result = context.Format(msg, null, errors);
            result.Should().Be("Zaktualizuj Firefoxa.");
            errors.Count.Should().Be(0);
        }

        [Test]
        public void UnknownVariantReturnsDefaultAndError()
        {
            var context = CreateContext(@"
                -term =
                    {
                        [a] A
                        *[b] B
                    }
                missing-term =
                    We should { -term[c] } select b and produce an error
            ");
            var errors = new List<FluentError>();
            var expectedErrors = new List<FluentError>()
            {
                new ReferenceError("Unknown variant: c")
            };
            var msg = context.GetMessage("missing-term");
            var result = context.Format(msg, null, errors);
            result.Should().Be("We should B select b and produce an error");
            errors.Should().BeEquivalentTo(expectedErrors);
        }

        [Test]
        public void NumberFormatTest()
        {
            var context = CreateContext(@"
                emails = You have { NUMBER($unreadEmails) } unread emails.
            ");
            var errors = new List<FluentError>();
            var msg = context.GetMessage("emails");
            var args = new Dictionary<string, object>()
            {
                { "unreadEmails", 42 }
            };
            var result = context.Format(msg, args, errors);
            result.Should().Be("You have 42 unread emails.");
            errors.Count.Should().Be(0);
        }

        [Test]
        public void DateFormatTest()
        {
            var context = CreateContext(@"
                last-notice =
                    Last checked: { DATETIME($lastChecked) }.
            ");
            var errors = new List<FluentError>();
            var msg = context.GetMessage("last-notice");
            var args = new Dictionary<string, object>()
            {
                { "lastChecked", new DateTime(2018, 7, 25, 17, 18, 0) }
            };
            var result = context.Format(msg, args, errors);
            result.Should().Be("Last checked: 7/25/2018 5:18:00 PM.");
            errors.Count.Should().Be(0);
        }

        [Test]
        public void FunctionThatDoesntExistReturnsAnError()
        {
            var context = CreateContext(@"
                no-fun =
                    I'm not having { NOFUN() }.
            ");
            var errors = new List<FluentError>();
            var expectedErrors = new List<FluentError>()
            {
                new ReferenceError("Unknown function: NOFUN()")
            };
            var msg = context.GetMessage("no-fun");
            var result = context.Format(msg, null, errors);
            result.Should().Be("I'm not having NOFUN().");
            errors.Should().BeEquivalentTo(expectedErrors,
                opts => opts.RespectingRuntimeTypes());
        }

        [Test]
        public void TermThatDoesntExistReturnsAnError()
        {
            var context = CreateContext(@"
                no-term = { -term } that doesn't exist
            ");
            var errors = new List<FluentError>();
            var expectedErrors = new List<FluentError>()
            {
                new ReferenceError("Unknown term: -term")
            };
            var msg = context.GetMessage("no-term");
            var result = context.Format(msg, null, errors);
            result.Should().Be("-term that doesn't exist");
            errors.Should().BeEquivalentTo(expectedErrors,
                opts => opts.RespectingRuntimeTypes());
        }

        [Test]
        public void MessageThatDoesntExistReturnsAnError()
        {
            var context = CreateContext(@"
                no-message = { message } that doesn't exist
            ");
            var errors = new List<FluentError>();
            var expectedErrors = new List<FluentError>()
            {
                new ReferenceError("Unknown message: message")
            };
            var msg = context.GetMessage("no-message");
            var result = context.Format(msg, null, errors);
            result.Should().Be("message that doesn't exist");
            errors.Should().BeEquivalentTo(expectedErrors,
                opts => opts.RespectingRuntimeTypes());
        }

        [Test]
        public void MessageWithVariables()
        {
            var context = CreateContext(@"
                variables = This message has variables: string = { $stringArg } and number = { $numberArg }
            ");
            var errors = new List<FluentError>();
            var msg = context.GetMessage("variables");
            var args = new Dictionary<string, object>()
            {
                { "stringArg", "test string" },
                { "numberArg", "1.234" }
            };
            var result = context.Format(msg, args, errors);
            result.Should().Be("This message has variables: string = test string and number = 1.234");
            errors.Count.Should().Be(0);
        }

        [Test]
        public void MessageWithMissingVariables()
        {
            var context = CreateContext(@"
                variables = This message has variables: string = { $stringArg } and number = { $numberArg }
            ");
            var errors = new List<FluentError>();
            var expectedErrors = new List<FluentError>()
            {
                new ReferenceError("Unknown variable: $stringArg"),
                new ReferenceError("Unknown variable: $numberArg")
            };
            var msg = context.GetMessage("variables");
            var args = new Dictionary<string, object>();
            var result = context.Format(msg, args, errors);
            result.Should().Be("This message has variables: string = stringArg and number = numberArg");
            errors.Should().BeEquivalentTo(expectedErrors,
                opts => opts.RespectingRuntimeTypes());
        }

        [Test]
        public void MessageWithNullVariables()
        {
            var context = CreateContext(@"
                variables = This message has a null variable: string = { $stringArg }
            ");
            var errors = new List<FluentError>();
            var expectedErrors = new List<FluentError>()
            {
                new TypeError("Unsupported variable type: stringArg, null"),
            };
            var msg = context.GetMessage("variables");
            var args = new Dictionary<string, object>()
            {
                { "stringArg", null },
            };
            var result = context.Format(msg, args, errors);
            result.Should().Be("This message has a null variable: string = stringArg");
            errors.Should().BeEquivalentTo(expectedErrors,
                opts => opts.RespectingRuntimeTypes());
        }

    }
}
