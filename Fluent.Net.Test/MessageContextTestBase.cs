using FluentAssertions;
using System.Collections.Generic;
using System.Globalization;

namespace Fluent.Net.Test
{
    public class MessageContextTestBase : FtlTestBase
    {
        protected static MessageContext CreateContext(string ftl, bool useIsolating = false)
        {
            var ctx = new MessageContext(new CultureInfo("en-US", useUserOverride: false), new MessageContextOptions()
                { UseIsolating = useIsolating });
            var errors = ctx.AddMessages(Ftl(ftl));
            errors.Should().BeEquivalentTo(new List<ParseException>());
            return ctx;
        }
    }
}
