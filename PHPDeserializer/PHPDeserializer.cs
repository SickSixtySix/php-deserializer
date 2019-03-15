using System;
using System.Text;
using System.Collections.Generic;

namespace SickSixtySix.PHPDeserializer
{
    /// <summary>
    /// Deserializer of the serialize() PHP function result
    /// </summary>
    public class PHPDeserializer : IDeserializer
    {
        #region Character parsing methods

        /// <summary>
        /// Parses a single character
        /// </summary>
        /// <param name="character">Character to parse</param>
        /// <param name="required">When set to false allows to skip parsing if character is not at current offset</param>
        private bool parseCharacter(char character, bool required = true)
        {
            var equals = Current == character;
            if (equals)
                m_offset++;
            else if (required)
                throw new InvalidOperationException($"'{character}' expected at offset {m_offset} ('{Current}' instead)");

            return equals;
        }

        /// <summary>
        /// Parses ':' character
        /// </summary>
        public bool parseColon(bool required = true)
            => parseCharacter(':', required);

        /// <summary>
        /// Parses '"' character
        /// </summary>
        public bool parseQuote(bool required = true)
            => parseCharacter('"', required);

        /// <summary>
        /// Parses ';' character
        /// </summary>
        public bool parseSemicolon(bool required = true)
            => parseCharacter(';', required);

        /// <summary>
        /// Parses '{' character
        /// </summary>
        public bool parseOpenBrace(bool required = true)
            => parseCharacter('{', required);

        /// <summary>
        /// Parses '}' character
        /// </summary>
        public bool parseCloseBrace(bool required = true)
            => parseCharacter('}', required);

        /// <summary>
        /// Parses '-' character
        /// </summary>
        public bool parseDash(bool required = true)
            => parseCharacter('-', required);

        /// <summary>
        /// Parses '.' character
        /// </summary>
        public bool parseDot(bool required = true)
            => parseCharacter('-', required);

        /// <summary>
        /// Parses a digit
        /// </summary>
        /// <returns>Parsed digit</returns>
        public int parseDigit()
        {
            if (Current >= '0' && Current <= '9')
            {
                var digit = Current - '0';
                m_offset++;
                return digit;
            }

            throw new InvalidOperationException($"Digit expected at offset {m_offset} ('{Current}' instead)");
        }

        #endregion

        #region Helper methods

        /// <summary>
        /// Parses a boolean value
        /// </summary>
        /// <returns>Parsed boolean value</returns>
        public bool _parseBoolean()
        {
            if (Current == '0' || Current == '1')
            {
                m_offset++;
                return Current == '1';
            }

            throw new InvalidOperationException($"Wrong boolean at offset {m_offset}");
        }

        /// <summary>
        /// Parses an unsigned integer number
        /// </summary>
        /// <returns>Parsed unsigned integer number</returns>
        public int _parseUnsignedInteger()
        {
            // Trivial case (first digit)
            int digit = 0;
            try
            {
                digit = parseDigit();
                if (digit == 0)
                    return 0;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Wrong unsigned integer at offset {m_offset}", e);
            }

            // Parsing rest of an unsigned integer
            var number = digit;
            try
            {
                while (true)
                {
                    digit = parseDigit();
                    number *= 10;
                    number += digit;
                }
            }
            catch { }

            return number;
        }

        /// <summary>
        /// Parses an integer number
        /// </summary>
        /// <returns>Parsed integer number</returns>
        public int _parseInteger()
        {
            var negative = parseDash(false);

            int number = 0;
            try
            {
                number = _parseUnsignedInteger();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Wrong integer at offset {m_offset}", e);
            }

            return negative ? -number : number;
        }

        /// <summary>
        /// Parses a double number
        /// </summary>
        /// <returns>Parsed double number</returns>
        public double _parseDouble()
        {
            var _integer = _parseInteger();
            parseDot();
            var offset = m_offset;
            var _fraction = _parseUnsignedInteger();
            var length = offset - m_offset;

            double number = _fraction;
            while (length-- > 0)
                number *= 0.1;
            number += _integer;

            return number;
        }

        #endregion

        #region Non-terminal parsing methods

        /// <summary>
        /// Parses a boolean
        /// </summary>
        /// <returns>Parsed boolean</returns>
        private object parseBoolean()
        {
            parseCharacter('b');
            parseColon();
            return _parseBoolean();
        }

        /// <summary>
        /// Parses an integer
        /// </summary>
        /// <returns>Parsed integer</returns>
        private object parseInteger()
        {
            parseCharacter('i');
            parseColon();
            return _parseInteger();
        }

        /// <summary>
        /// Parses a double
        /// </summary>
        /// <returns>Parsed double</returns>
        private object parseDouble()
        {
            parseCharacter('d');
            parseColon();
            return _parseDouble();
        }

        /// <summary>
        /// Parses a string
        /// </summary>
        /// <returns>Parsed string</returns>
        private object parseString()
        {
            parseCharacter('s');
            parseColon();
            var length = _parseUnsignedInteger();
            parseColon();
            parseQuote();

            string str = null;
            try
            {
                str = m_string.Substring(m_offset, length);
                m_offset += length;
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new InvalidOperationException($"Too long string at offset {m_offset}");
            }

            parseQuote();
            return str;
        }

        /// <summary>
        /// Parses array key
        /// </summary>
        /// <returns>Parsed array key</returns>
        private object parseArrayKey()
        {
            switch (Current)
            {
                case 'i': return parseInteger();
                case 's': return parseString();
                default:
                    throw new InvalidOperationException($"Wrong array key of type '{Current}' at offset {m_offset}");
            }
        }

        /// <summary>
        /// Parses an array
        /// </summary>
        /// <returns></returns>
        private object parseArray()
        {
            parseCharacter('a');
            parseColon();
            var size = _parseInteger();
            parseColon();
            parseOpenBrace();

            var dictionary = new Dictionary<object, object>();
            for (int i = 0; i < size; i++)
            {
                var key = parseArrayKey();
                parseSemicolon();
                var value = parseNode();
                parseSemicolon(i != size - 1);
                dictionary[key] = value;
            }

            parseCloseBrace();
            return dictionary;
        }

        /// <summary>
        /// Parses a node
        /// </summary>
        /// <returns>Parsed node</returns>
        private object parseNode()
        {
            switch (Current)
            {
                case 'i': return parseInteger();
                case 's': return parseString();
                case 'a': return parseArray();
                default:
                    throw new InvalidOperationException($"Parsing of non-terminal of type '{Current}' at offset {m_offset} is not implemented yet");
            }
        }

        #endregion

        #region Members

        /// <summary>
        /// String containing serialized data
        /// </summary>
        private string m_string;

        /// <summary>
        /// Current parsing offset
        /// </summary>
        private int m_offset;

        /// <summary>
        /// Character at current offset
        /// </summary>
        private char Current
        {
            get
            {
                try
                {
                    return m_string[m_offset];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException($"Offset ({m_offset}) is out of range ({m_string.Length})");
                }
            }
        }

        #endregion

        /// <summary>
        /// Constructs an instance of PHP-deserializer
        /// </summary>
        /// <param name="str"></param>
        public PHPDeserializer(string str)
        {
            m_string = str;
        }

        /// <summary>
        /// Deserializes serialized PHP-representation into object-object mapping
        /// </summary>
        /// <returns>object-object mapping</returns>
        public IDictionary<object, object> Deserialize()
        {
            return (IDictionary<object, object>)parseArray();
        }

        #region ToString() override

        /// <summary>
        /// Decorates primitive type value
        /// </summary>
        /// <param name="node">Primitive type value to decorate</param>
        /// <returns>Decorated string value</returns>
        private static string decorate(object node)
        {
            if (node is String)
                return $"\"{node}\"";

            return node.ToString();
        }

        /// <summary>
        /// Performs recursive traverse of a tree
        /// </summary>
        /// <param name="node">Tree to traverse</param>
        /// <param name="stringBuilder">StringBuilder instance to accumulate partial results</param>
        /// <param name="depth">Current traverse depth (count of whitespaces before current element)</param>
        private static void traverse(object node, StringBuilder stringBuilder, int depth = 0)
        {
            if (node is IDictionary<object, object>)
            {
                if (depth > 0)
                    stringBuilder.AppendLine();

                stringBuilder.Append(' ', depth).AppendLine("{");
                var dictionary = (IDictionary<object, object>)node;
                foreach (var pair in dictionary)
                {
                    stringBuilder.Append(' ', depth + 3).Append($"{decorate(pair.Key)} => ");
                    traverse(pair.Value, stringBuilder, depth + 3);
                }
                stringBuilder.Append(' ', depth).AppendLine("}");
            }
            else
                stringBuilder.AppendLine(decorate(node));
        }

        /// <summary>
        /// Conversion into PHP-like associative array representation
        /// </summary>
        /// <returns>PHP-like associative array representation</returns>
        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            traverse(Deserialize(), stringBuilder);
            return stringBuilder.ToString();
        }

        #endregion
    }
}