using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Fluent.Net.Ast;

namespace Fluent.Net
{

#if FALSE
    /// <summary>
    /// The `Parser` class is responsible for parsing FTL resources.
    /// 
    /// It's only public method is `getResource(source)` which takes an FTL string
    /// and returns a two element Array with an Object of entries generated from the
    /// source as the first element and an array of ParseException objects as the
    /// second.
    /// 
    /// This parser is optimized for runtime performance.
    /// 
    /// There is an equivalent of this parser in syntax/parser which is
    /// generating full AST which is useful for FTL tools.
    /// </summary>
    public class Parser
    {
        const int MAX_PLACEABLES = 100;
        readonly static Regex entryIdentifierRe = new Regex("/-?[a-zA-Z][a-zA-Z0-9_-]*/y");
        readonly static Regex identifierRe = new Regex("/[a-zA-Z][a-zA-Z0-9_-]*/y");
        readonly static Regex functionIdentifierRe = new Regex("/^[A-Z][A-Z_?-]*$/");

        string _source;
        int _index;

        int _length { get { return _length; } }
        int Peek(int n = 0)
        {
            return _index < _length - n ? _source[_index + n] : -1;
        }

        int SkipAndPeek(int n = 1)
        {
            _index = _index < _length - n ? _index + n : n;
            return Peek();
        }
                ++_index;
                ch = Peek();


        /// <summary>
        /// Parse FTL code into entries formattable by the MessageContext.
        /// 
        /// Given a string of FTL syntax, return a map of entries that can be passed
        /// to MessageContext.format and a list of errors encountered during parsing.
        /// </summary>
        /// <param name='ftl'>The ftl text</param>
        /// @returns {Array<Object, Array>}
        void GetResource(string ftl)
        {
            _source = ftl;
            _index = 0;

            // _length = string.length;
            // entries = {};

            // const errors = [];
            // 
            // SkipWS();
            // while (_index < _length) {
            //   try {
            //     getEntry();
            //   } catch (e) {
            //     if (e instanceof ParseException) {
            //       errors.push(e);
            // 
            //       skipToNextEntryStart();
            //     } else {
            //       throw e;
            //     }
            //   }
            //   SkipWS();
            // }
            // 
            // return [entries, errors];
        }

        /**
        * Parse the source string from the current index as an FTL entry
        * and add it to object's entries property.
        *
        * @private
        */
        void GetEntry()
        {
            // The index here should either be at the beginning of the file
            // or right after new line.
            if (_index != 0 && _source[_index - 1] != '\n')
            {
                error("Expected an entry to start " +
                    " at the beginning of the file or on a new line.");
            }

            var ch = _source[_index];

            // We don't care about comments or sections at runtime
            if (ch == '/' ||
                (ch == '#' && _index + 1 < _length &&
                " #\n".IndexOf(_source[_index + 1]) >= 0))
            {
                SkipComment();
                return;
            }

            if (ch == '[') {
                SkipSection();
                return;
            }

            GetMessage();
        }

        /**
        * Skip the section entry from the current index.
        *
        * @private
        */
        void SkipSection()
        {
            _index += 1;
            if (_source[_index] != '[')
            {
                error("Expected '[[' to open a section");
            }

            _index += 1;

            SkipInlineWS();
            GetVariantName();
            SkipInlineWS();

            if (_source[_index] != ']' || _index + 1 >= _length ||
                _source[_index + 1] != ']')
            {
                error("Expected ']]' to close a section");
            }

            _index += 2;
        }

        /**
        * Parse the source string from the current index as an FTL message
        * and add it to the entries property on the Parser.
        *
        * @private
        */
        void GetMessage()
        {
            var id = GetEntryIdentifier();

            SkipInlineWS();

            if (_source[_index] == '=')
            {
                _index++;
            }

            SkipInlineWS();

            var val = GetPattern();

            if (id.StartsWith("-") && val == null)
            {
                error("Expected term to have a value");
            }

            let attrs = null;

            if (_source[_index] == ' ')
            {
                int lineStart = _index;
                SkipInlineWS();

                if (_source[_index] == '.')
                {
                    _index = lineStart;
                    attrs = GetAttributes();
                }
            }

            if (attrs == null && typeof val == 'string')
            {
                entries[id] = val;
            }
            else
            {
                if (val == null && attrs == null)
                {
                    error("Expected message to have a value or attributes");
                }

                entries[id] = {};

                if (val != null)
                {
                    entries[id].val = val;
                }

                if (attrs != null)
                {
                    entries[id].attrs = attrs;
                }
            }
        }

        /**
        * Skip whitespace.
        *
        * @private
        */
        void SkipWS()
        {
            var ch = _source[_index];
            while (ch == ' ' || ch == '\n' || ch == '\t' || ch == '\r') {
                ch = _source[++_index];
            }
        }

        bool IsWhite(char c)
        {
            return c == ' ' || c == '\t';
        }

        /**
        * Skip inline whitespace (space and \t).
        *
        * @private
        */
        void SkipInlineWS()
        {
            for (; _index < _length &&
                IsWhite(_source[_index]); ++_index)
            {
            }
        }

        /**
        * Skip blank lines.
        *
        * @private
        */
        void SkipBlankLines()
        {
            while (true) {
                var ptr = _index;

                SkipInlineWS();

                if (_index < _length && _source[_index] == '\n')
                {
                    _index += 1;
                }
                else
                {
                    _index = ptr;
                    break;
                }
            }
        }

        /**
        * Get identifier using the provided regex.
        *
        * By default this will get identifiers of public messages, attributes and
        * external arguments (without the $).
        *
        * @returns {String}
        * @private
        */
        string GetIdentifier(Regex re = null)
        {
            re = re ?? identifierRe;
            Match m = re.Match(_source, _index);

            if (m == null) 
            {
                _index += 1;
                error(
                    $"Expected an identifier [${re}]");
            }

            _index = m.Index + m.Length;
            return m.Value;
        }

        /**
        * Get identifier of a Message or a Term (staring with a dash).
        *
        * @returns {String}
        * @private
        */
        string getEntryIdentifier()
        {
            return GetIdentifier(entryIdentifierRe);
        }

        bool IsVariantLeader(char c)
        {
            return
                (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                c == '_' || c == ' ';
        }

        bool IsVariantChar(char c)
        {
            return
                IsVariantLeader(c) ||
                (c >= '0' && c <= '9') ||
                c == '-';
        }

        /**
        * Get Variant name.
        *
        * @returns {Object}
        * @private
        */
        void GetVariantName()
        {
            string name = "";

            var start = _index;

            if (_index >= _length ||
                !IsVariantLeader(_source[_index]))
            {
                error(
                    "Expected a keyword (starting with [a-zA-Z_])");
            }

            for (++_index; _index < _length &&
                 IsVariantChar(_source[_index]); ++_index)
            {
            }

            // If we encountered the end of name, we want to test if the last
            // collected character is a space.
            // If it is, we will backtrack to the last non-space character because
            // the keyword cannot end with a space character.
            for (; _index > start && _source[_index - 1] == ' '; --_index)
            {
            }

            name += _source.Substring(start, _index - start);

            // return { type: 'varname', name };
        }

        /**
        * Get simple string argument enclosed in `'`.
        *
        * @returns {String}
        * @private
        */
        string GetString()
        {
            var start = _index + 1;

            while (++_index < _length)
            {
                var ch = _source[_index];

                if (ch == '\'')
                {
                    break;
                }

                if (ch == '\n')
                {
                    error("Unterminated string expression");
                }
            }
            return _source.Substring(start, _index++ - start);
        }

        /**
        * Parses a Message pattern.
        * Message Pattern may be a simple string or an array of strings
        * and placeable expressions.
        *
        * @returns {String|Array}
        * @private
        */
        string GetPattern()
        {
            // We're going to first try to see if the pattern is simple.
            // If it is we can just look for the end of the line and read the string.
            //
            // Then, if either the line contains a placeable opening `{` or the
            // next line starts an indentation, we switch to complex pattern.
            int start = _index;
            int eol = _source.IndexOf('\n', _index);

            if (eol == -1)
            {
                eol = _length;
            }

            string firstLineContent = _source.Substring(start, eol - start);

            if (firstLineContent.IndexOf('{') >= 0)
            {
                return GetComplexPattern();
            }

            _index = eol + 1;

            SkipBlankLines();

            if (_index < _length && _source[_index] != ' ')
            {
                // No indentation means we're done with this message. Callers should check
                // if the return value here is null. It may be OK for messages, but not OK
                // for terms, attributes and variants.
                return firstLineContent;
            }

            int lineStart = _index;

            SkipInlineWS();

            if (_index < _length && _source[_index] == '.')
            {
                // The pattern is followed by an attribute. Rewind _index to the first
                // column of the current line as expected by GetAttributes.
                _index = lineStart;
                return firstLineContent;
            }

            if (firstLineContent.Length > 0)
            {
                // It's a multiline pattern which started on the same line as the
                // identifier. Reparse the whole pattern to make sure we get all of it.
                _index = start;
            }

            return GetComplexPattern();
        }

        /**
        * Parses a complex Message pattern.
        * This function is called by GetPattern when the message is multiline,
        * or contains escape chars or placeables.
        * It does full parsing of complex patterns.
        *
        * @returns {Array}
        * @private
        */
        IEnumerable<string> GetComplexPattern()
        {
            var buffer = new StringBuilder();
            var content = new List<string>();
            int placeables = 0;

            // let ch = _source[_index];

            while (_index < _length)
            {
                char ch = _source[_index];

                // This block handles multi-line strings combining strings separated
                // by new line.
                if (ch == '\n')
                {
                    _index++;

                    // We want to capture the start and end pointers
                    // around blank lines and add them to the buffer
                    // but only if the blank lines are in the middle
                    // of the string.
                    int blankLinesStart = _index;
                    SkipBlankLines();
                    int blankLinesEnd = _index;

                    if (_index >= _length || _source[_index] != ' ')
                    {
                        break;
                    }
                    SkipInlineWS();

                    if (_index < _length && (
                        _source[_index] == '}' ||
                        _source[_index] == '[' ||
                        _source[_index] == '*' ||
                        _source[_index] == '.'))
                    {
                        _index = blankLinesEnd;
                        break;
                    }

                    buffer.Append(_source, blankLinesStart, blankLinesEnd - blankLinesStart);

                    if (buffer.Length > 0 || content.Count > 0) {
                        buffer.Append('\n');
                    }
                    continue;
                }
                // check if it's a valid escaped thing
                else if (ch == '\\') 
                {
                    if (_index + 1 < _length)
                    {
                        char ch2 = _source[_index + 1];
                        if (ch2 == '\'' || ch2 == '{' || ch2 == '\\')
                        {
                            ch = ch2;
                            ++_index;
                        }
                    }
                }
                else if (ch == '{')
                {
                    // Push the buffer to content array right before placeable
                    if (buffer.Length > 0)
                    {
                        content.Add(buffer.ToString());
                    }
                    if (placeables > MAX_PLACEABLES - 1)
                    {
                        error(
                            $"Too many placeables, maximum allowed is {MAX_PLACEABLES}");
                    }
                    buffer.Length = 0;
                    content.Add(GetPlaceable());

                    _index++;
                    placeables++;
                    continue;
                }

                buffer.Append(ch);
                _index++;
                ch = _source[_index];
            }

            if (buffer.Length > 0)
            {
                content.Add(buffer.ToString());
            }

            return content;
        }

        /**
        * Parses a single placeable in a Message pattern and returns its
        * expression.
        *
        * @returns {Object}
        * @private
        */
        string GetPlaceable()
        {
            int start = ++_index;

            SkipWS();

            if (_index < _length &&
                (_source[_index] == '*' ||
                (_source[_index] == '[' &&
                 _index + 1 < _length && 
                 _source[_index + 1] != ']')))
            {
                const variants = GetVariants();

                /* TODO
                return {
                type: 'sel',
                exp: null,
                vars: variants[0],
                def: variants[1]
                };*/
            }

            // Rewind the index and only support in-line white-space now.
            _index = start;
            SkipInlineWS();

            const selector = getSelectorExpression();

            SkipWS();

            int ch = Peek();

            if (ch == '}')
            {
                if (selector.type == 'attr' && selector.id.name.startsWith('-'))
                {
                    error(
                        "Attributes of private messages cannot be interpolated.");
                }

                return selector;
            }

            if (ch != '-' || Peek(1) != '>')
            {
                error("Expected '}' or '->'");
            }

            if (selector.type == 'ref')
            {
                error("Message references cannot be used as selectors.");
            }

            if (selector.type == 'var')
            {
                error("Variants cannot be used as selectors.");
            }

            if (selector.type == 'attr' && !selector.id.name.startsWith('-'))
            {
                error(
                    "Attributes of public messages cannot be used as selectors."
                );
            }


            _index += 2; // ->

            SkipInlineWS();

            if (Peek() != '\n')
            {
                error("Variants should be listed in a new line");
            }

            SkipWS();

            const variants = GetVariants();

            if (variants[0].length == 0)
            {
                error("Expected members for the select expression");
            }

            return {
                type: 'sel',
                exp: selector,
                vars: variants[0],
                def: variants[1]
            };
        }

        /**
        * Parses a selector expression.
        *
        * @returns {Object}
        * @private
        */
        getSelectorExpression()
        {
            const literal = getLiteral();

            if (literal.type != 'ref')
            {
                return literal;
            }

            if (_source[_index] == '.') {
                _index++;

                const name = GetIdentifier();
                _index++;
                return {
                type: 'attr',
                id: literal,
                name
                };
            }

            if (_source[_index] == '[') {
                _index++;

                const key = getVariantKey();
                _index++;
                return {
                type: 'var',
                id: literal,
                key
                };
            }

            if (_source[_index] == '(') {
                _index++;
                const args = getCallArgs();

                if (!functionIdentifierRe.test(literal.name)) {
                error("Function names must be all upper-case");
                }

                _index++;

                literal.type = 'fun';

                return {
                type: 'call',
                fun: literal,
                args
                };
            }

            return literal;
        }

        /**
        * Parses call arguments for a CallExpression.
        *
        * @returns {Array}
        * @private
        */
        getCallArgs() {
        const args = [];

        while (_index < _length) {
            SkipInlineWS();

            if (_source[_index] == ')') {
            return args;
            }

            const exp = getSelectorExpression();

            // MessageReference in this place may be an entity reference, like:
            // `call(foo)`, or, if it's followed by `:` it will be a key-value pair.
            if (exp.type != 'ref') {
            args.push(exp);
            } else {
            SkipInlineWS();

            if (_source[_index] == ':') {
                _index++;
                SkipInlineWS();

                const val = getSelectorExpression();

                // If the expression returned as a value of the argument
                // is not a quote delimited string or number, throw.
                //
                // We don't have to check here if the pattern is quote delimited
                // because that's the only type of string allowed in expressions.
                if (typeof val == 'string' ||
                    Array.isArray(val) ||
                    val.type == 'num') {
                args.push({
                    type: 'narg',
                    name: exp.name,
                    val
                });
                } else {
                _index = _source.lastIndexOf(':', _index) + 1;
                error(
                    'Expected string in quotes, number.');
                }

            } else {
                args.push(exp);
            }
            }

            SkipInlineWS();

            if (_source[_index] == ')') {
            break;
            } else if (_source[_index] == ',') {
            _index++;
            } else {
            error("Expected ',' or ')'");
            }
        }

        return args;
        }

        /**
        * Parses an FTL Number.
        *
        * @returns {Object}
        * @private
        */
        string getNumber()
        {
            var num = new StringBuilder();
            int ch = Peek();

            // The number literal may start with negative sign `-`.
            if (ch == 45) {
                num.Append('-');
                ch = SkipAndPeek();
            }

            // next, we expect at least one digit
            if (ch < '0' || ch > '9') {
                error($"Unknown literal '{ch}'");
            }

            // followed by potentially more digits
            do
            {
                num.Append((char)ch);
                ++_index;
                ch = Peek();
            }
            while (ch >= '0' && ch <= '9');

            // followed by an optional decimal separator `.`
            if (ch == '.')
            {
                num += _source[_index++];
                cc = _source.charCodeAt(_index);

                // followed by at least one digit
                if (cc < 48 || cc > 57) {
                error($"Unknown literal '{num}'");
                }

                // and optionally more digits
                while (cc >= 48 && cc <= 57) {
                num += _source[_index++];
                cc = _source.charCodeAt(_index);
                }
            }

            return {
                type: 'num',
                val: num
            };
        }

        /**
        * Parses a list of Message attributes.
        *
        * @returns {Object}
        * @private
        */
        IDictionary<string, string> GetAttributes() {
            var attrs = new Dictionary<string, string>();

            while (_index < _length)
            {
                var ch = Peek();
                if (ch != ' ')
                {
                    break;
                }
                SkipInlineWS();

                if (ch != '.')
                {
                    break;
                }
                _index++;

                var key = GetIdentifier();

                SkipInlineWS();
                
                ch = Peek();
                if (ch != '=')
                {
                    error("Expected '='");
                }
                _index++;

                SkipInlineWS();

                var val = GetPattern();

                if (val == null)
                {
                    error("Expected attribute to have a value");
                }

                attrs[key] = val;
                /* TODO:
                if (typeof val == 'string') {
                attrs[key] = val;
                } else {
                attrs[key] = {
                    val
                };
                }*/

                SkipBlankLines();
            }

            return attrs;
        }

        /**
        * Parses a list of Selector variants.
        *
        * @returns {Array}
        * @private
        */
        GetVariants() {
            const variants = [];
            let index = 0;
            let defaultIndex;

            while (_index < _length) {
                const ch = _source[_index];

                if ((ch != '[' || _source[_index + 1] == '[') &&
                    ch != '*') {
                break;
                }
                if (ch == '*') {
                _index++;
                defaultIndex = index;
                }

                if (_source[_index] != '[') {
                error("Expected '['");
                }

                _index++;

                const key = getVariantKey();

                SkipInlineWS();

                const val = GetPattern();

                if (val == null) {
                error("Expected variant to have a value");
                }

                variants[index++] = {key, val};

                SkipWS();
            }

            return [variants, defaultIndex];
        }

        /**
        * Parses a Variant key.
        *
        * @returns {String}
        * @private
        */
        getVariantKey() {
        // VariantKey may be a Keyword or Number

        const cc = _source.charCodeAt(_index);
        let literal;

        if ((cc >= 48 && cc <= 57) || cc == 45) {
            literal = getNumber();
        } else {
            literal = GetVariantName();
        }

        if (_source[_index] != ']') {
            error("Expected ']'");
        }

        _index++;
        return literal;
        }

        /**
        * Parses an FTL literal.
        *
        * @returns {Object}
        * @private
        */
        getLiteral() {
        const cc0 = _source.charCodeAt(_index);

        if (cc0 == 36) { // $
            _index++;
            return {
            type: 'ext',
            name: GetIdentifier()
            };
        }

        const cc1 = cc0 == 45 // -
            // Peek at the next character after the dash.
            ? _source.charCodeAt(_index + 1)
            // Or keep using the character at the current index.
            : cc0;

        if ((cc1 >= 97 && cc1 <= 122) || // a-z
            (cc1 >= 65 && cc1 <= 90)) { // A-Z
            return {
            type: 'ref',
            name: getEntryIdentifier()
            };
        }

        if ((cc1 >= 48 && cc1 <= 57)) { // 0-9
            return getNumber();
        }

        if (cc0 == 34) { // '
            return GetString();
        }

        error("Expected literal");
        }

        /**
        * Skips an FTL comment.
        *
        * @private
        */
        SkipComment() {
        // At runtime, we don't care about comments so we just have
        // to parse them properly and skip their content.
        let eol = _source.indexOf('\n', _index);

        while (eol != -1 &&
            ((_source[eol + 1] == '/' && _source[eol + 2] == '/') ||
            (_source[eol + 1] == '#' &&
                [' ', '#'].includes(_source[eol + 2])))) {
            _index = eol + 3;

            eol = _source.indexOf('\n', _index);

            if (eol == -1) {
            break;
            }
        }

        if (eol == -1) {
            _index = _length;
        } else {
            _index = eol + 1;
        }
        }

        /**
        * Creates a error object with a given message.
        *
        * @param {String} message
        * @returns {Object}
        * @private
        */
        static void error(string message)
        {
            throw new ParseException(message);
        }

        /**
        * Skips to the beginning of a next entry after the current position.
        * This is used to mark the boundary of junk entry in case of error,
        * and recover from the returned position.
        *
        * @private
        */
        skipToNextEntryStart() {
        let start = _index;

        while (true) {
            if (start == 0 || _source[start - 1] == '\n') {
            const cc = _source.charCodeAt(start);

            if ((cc >= 97 && cc <= 122) || // a-z
                (cc >= 65 && cc <= 90) || // A-Z
                    cc == 47 || cc == 91) { // /[
                _index = start;
                return;
            }
            }

            start = _source.indexOf('\n', start);

            if (start == -1) {
            _index = _length;
            return;
            }
            start++;
        }
        }
}

/**
 * Parses an FTL string using RuntimeParser and returns the generated
 * object with entries and a list of errors.
 *
 * @param {String} string
 * @returns {Array<Object, Array>}
 */
default function parse(string) {
  const parser = new RuntimeParser();
  return parser.getResource(string);
}

    }
#endif
}
