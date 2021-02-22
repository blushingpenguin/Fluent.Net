namespace Fluent.Net
{
    public abstract class FluentError
    {
        public string Message { get; set; }

        public FluentError(string message)
        {
            Message = message;
        }
    }

    class RangeError : FluentError
    {
        public RangeError(string message) :
            base(message)
        {
        }
    }

    class TypeError : FluentError
    {
        public TypeError(string message) :
            base(message)
        {
        }
    }

    class ReferenceError : FluentError
    {
        public ReferenceError(string message) :
            base(message)
        {
        }
    }
}