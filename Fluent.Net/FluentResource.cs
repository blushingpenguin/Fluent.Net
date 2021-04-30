using System.Collections.Generic;
using System.IO;
using Fluent.Net.RuntimeAst;

namespace Fluent.Net
{
    /// <summary>
    /// Fluent Resource is a structure storing a map
    /// of localization entries.
    /// </summary>
    public class FluentResource
    {
        public IDictionary<string, Message> Entries { get; }
        public IList<ParseException> Errors { get; }

        public FluentResource(IDictionary<string, Message> entries,
            IList<ParseException> errors)
        {
            Entries = entries;
            Errors = errors;
        }

        public static FluentResource FromReader(TextReader reader)
        {
            var parser = new RuntimeParser();
            var resource = parser.GetResource(reader);
            return new FluentResource(resource.Entries, resource.Errors);
        }
    }
}