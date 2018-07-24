using FluentAssertions;
using System.Collections.Generic;

namespace Fluent.Net.Test
{
    public class MessageContextTestBase : FtlTestBase
    {
        protected static MessageContext CreateContext(string ftl, bool useIsolating = false)
        {
            var locales = new string[] { "en-US", "en" };
            var ctx = new MessageContext(locales, new MessageContextOptions()
                { UseIsolating = useIsolating });
            var errors = ctx.AddMessages(Ftl(ftl));
            errors.Should().BeEquivalentTo(new List<ParseException>());
            return ctx;
        }
    }
}
