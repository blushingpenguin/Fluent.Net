using System;
using System.Collections.Generic;

namespace Fluent.Net
{
    internal static class BuiltIns
    {
        public static FluentType Number(IList<object> args, IDictionary<string, object> options)
        {
            // TODO: add to errors?  what we doin here?
            if (args.Count != 1)
            {
                throw new Exception("Too many arguments to NUMBER() function");
            }
            if (args[0].GetType() != typeof(FluentNumber))
            {
                throw new Exception("NUMBER() expected an argument of type FluentNumber");
            }
            return (FluentNumber)args[0];
        }

        public static FluentType DateTime(IList<object> args, IDictionary<string, object> options)
        {
            if (args.Count != 1)
            {
                throw new Exception("Too many arguments to DATETIME() function");
            }
            if (args[0].GetType() != typeof(FluentDateTime))
            {
                throw new Exception("DATETIME() expected an argument of type FluentDateTime");
            }
            return (FluentDateTime)args[0];
        }
    }
}