using System;
using Fluent.Net.RuntimeAst;

namespace Fluent.Net
{
    public interface IFluentType
    {
        string Value { get; set; }
        string Format(MessageContext ctx);
        bool Match(MessageContext ctx, object obj);
    }

    /**
     * The `FluentType` class is the base of Fluent's type system.
     *
     * Fluent types wrap JavaScript values and store additional configuration for
     * them, which can then be used in the `toString` method together with a proper
     * `Intl` formatter.
     */
    public abstract class FluentType : IFluentType
    {
        public string Value { get; set; }

        /**
         * Create an `FluentType` instance.
         *
         * @param   {Any}    value - JavaScript value to wrap.
         * @param   {Object} opts  - Configuration.
         * @returns {FluentType}
         */
        public FluentType(string value = null)
        {
            Value = value;
        }

        /**
         * Unwrap the raw value stored by this `FluentType`.
         *
         * @returns {Any}
        public string ValueOf()
        {
            return Value;
        }
         */
        /**
         * Format this instance of `FluentType` to a string.
         *
         * Formatted values are suitable for use outside of the `MessageContext`.
         * This method can use `Intl` formatters memoized by the `MessageContext`
         * instance passed as an argument.
         *
         * @param   {MessageContext} [ctx]
         * @returns {string}
         */
        public abstract string Format(MessageContext ctx);

        public abstract bool Match(MessageContext ctx, object obj);
    }

    public class FluentNone : Node, IFluentType
    {
        public string Value { get; set; }

        public FluentNone(string value = null)
        {
            Value = value;
        }

        public string Format(MessageContext ctx)
        {
            return !String.IsNullOrEmpty(Value) ? Value : "???";
        }

        public bool Match(MessageContext ctx, object other)
        {
            return other is FluentNone;
        }
    }

    public class FluentString : FluentType
    {
        public FluentString(string value) :
            base(value)
        {
        }

        public override string Format(MessageContext ctx)
        {
            return Value;
        }

        public override bool Match(MessageContext ctx, object other)
        {
            if (other is FluentString str)
            {
                return str.Value == Value;
            }

            if (other is string s)
            {
                return s == Value;
            }

            return false;
        }
    }

    public class FluentNumber : FluentType
    {
        double _numberValue;

        public FluentNumber(string value) :
            base(value)
        {
            _numberValue = Double.Parse(value);
        }

        public override string Format(MessageContext ctx)
        {
            // TODO: match js number formattiing here
            // System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo(
            return String.Format(ctx.Culture, "{0}", _numberValue);
        }

        /**
         * Compare the object with another instance of a FluentType.
         *
         * @param   {MessageContext} ctx
         * @param   {FluentType}     other
         * @returns {bool}
         */
        public override bool Match(MessageContext ctx, object other)
        {
            if (other is FluentNumber n)
            {
                return _numberValue == n._numberValue;
            }

            if (other is double d)
            {
                return _numberValue == d;
            }

            if (other is float f)
            {
                return (float) _numberValue == f;
            }

            if (other is decimal dec)
            {
                return _numberValue == (double) dec;
            }

            if (other is sbyte sb)
            {
                return _numberValue == sb;
            }

            if (other is short s)
            {
                return _numberValue == s;
            }

            if (other is int i)
            {
                return _numberValue == i;
            }

            if (other is long l)
            {
                return _numberValue == l;
            }

            if (other is byte b)
            {
                return _numberValue == b;
            }

            if (other is ushort us)
            {
                return _numberValue == us;
            }

            if (other is uint ui)
            {
                return _numberValue == ui;
            }

            if (other is ulong ul)
            {
                return _numberValue == ul;
            }

            return false;
        }
    }

    public class FluentDateTime : FluentType
    {
        DateTime _dateValue;

        public FluentDateTime(DateTime value) :
            base(value.ToString("o"))
        {
            _dateValue = value;
        }

        public override string Format(MessageContext ctx)
        {
            // TODO: match js number formattiig here?
            // System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo(
            return String.Format(ctx.Culture, "{0}", _dateValue);
        }

        public override bool Match(MessageContext ctx, object other)
        {
            if (other is FluentDateTime d)
            {
                return _dateValue == d._dateValue;
            }

            if (other is DateTime dt)
            {
                return _dateValue == dt;
            }

            return false;
        }
    }

    public class FluentSymbol : FluentType
    {
        public FluentSymbol(string value) :
            base(value)
        {
        }

        public override string Format(MessageContext ctx)
        {
            return Value;
        }

        /**
         * Compare the object with another instance of a FluentType.
         *
         * @param   {MessageContext} ctx
         * @param   {FluentType}     other
         * @returns {bool}
         */
        public override bool Match(MessageContext ctx, object other)
        {
            if (other is FluentSymbol symbol)
            {
                return Value == symbol.Value;
            }
            else if (other is string str)
            {
                return Value == str;
            }
            else if (other is FluentString fstr)
            {
                return Value == fstr.Value;
            }
            else if (other is FluentNumber fnum)
            {
                return Value == Plural.LocaleRules.Select(ctx.Locales, fnum.Value);
            }

            return false;
        }
    }
}