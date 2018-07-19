using System;

namespace Fluent.Net
{
    public class ParseException : Exception
    {
        public string Code { get; private set; }
        public string[] Args { get; private set; }

        public ParseException(string code, params string[] args) :
            this(code, null, args)
        {
        }

        public ParseException(string code, Exception innerException, params string[] args) :
            base(GetErrorMessage(code, args), innerException)
        {
            Code = code;
            Args = args;
        }

        static string GetErrorMessage(string code, string[] args)
        {
            switch (code)
            {
                case "E0001":
                    return "Generic error";
                case "E0002":
                    return "Expected an entry start";
                case "E0003":
                    return $"Expected token: \"{args[0]}\"";
                case "E0004":
                    return $"Expected a character from range: \"{args[0]}\"";
                case "E0005":
                    return $"Expected message \"{args[0]}\" to have a value or attributes";
                case "E0006":
                    return $"Expected term \"{args[0]}\" to have a value";
                case "E0007":
                    return "Keyword cannot end with a whitespace";
                case "E0008":
                    return "The callee has to be a simple, upper-case identifier";
                case "E0009":
                    return "The key has to be a simple identifier";
                case "E0010":
                    return "Expected one of the variants to be marked as default (*)";
                case "E0011":
                    return "Expected at least one variant after \"->\"";
                case "E0012":
                    return "Expected value";
                case "E0013":
                    return "Expected variant key";
                case "E0014":
                    return "Expected literal";
                case "E0015":
                    return "Only one variant can be marked as default (*)";
                case "E0016":
                    return "Message references cannot be used as selectors";
                case "E0017":
                    return "Variants cannot be used as selectors";
                case "E0018":
                    return "Attributes of messages cannot be used as selectors";
                case "E0019":
                    return "Attributes of terms cannot be used as placeables";
                case "E0020":
                    return "Unterminated string expression";
                default:
                    return code;
            }
        }
    }
}
