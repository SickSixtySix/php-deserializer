using System;
using System.Collections.Generic;

namespace SickSixtySix.PHPDeserializer
{
    /// <summary>
    /// Deserializer of the serialize() PHP function result
    /// </summary>
    public class PHPDeserializer : IDeserializer
    {
        #region Helper methods

        /// <summary>
        /// Parses an integer number
        /// </summary>
        /// <returns>Parsed unsigned integer number</returns>
        public int _parseNUM()
        {
            if (Current == '0')
            {
                m_offset++;
                return 0;
            }

            bool p = true;
            if (Current == '-')
            {
                p = false;
                m_offset++;
            }

            int number = 0;
            while (Current >= '0' && Current <= '9')
            {
                number *= 10;
                number += Current - '0';
                m_offset++;
            }

            return p ? number : -number;
        }

        #endregion

        #region Character parsing methods

        /// <summary>
        /// Parses a single character
        /// </summary>
        /// <param name="c">Character to parse</param>
        /// <param name="r">When set to false allows to skip parsing if character is not at current offset</param>
        private void parseCHR(char c, bool r = true)
        {
            if (Current == c)
                m_offset++;
            else if (r)
                throw new InvalidOperationException($"'{c}' expected at offset {m_offset} ('{Current}' instead)");
        }

        /// <summary>
        /// Parses ':' character
        /// </summary>
        public void parseCOL()
            => parseCHR(':');

        /// <summary>
        /// Parses '"' character
        /// </summary>
        public void parseQTE()
            => parseCHR('"');

        /// <summary>
        /// Parses ';' character
        /// </summary>
        public void parseSEM(bool r = true)
            => parseCHR(';', r);

        /// <summary>
        /// Parses '{' character
        /// </summary>
        public void parseOBR()
            => parseCHR('{');

        /// <summary>
        /// Parses '}' character
        /// </summary>
        public void parseCBR()
            => parseCHR('}');

        #endregion

        #region Non-terminal parsing methods

        /// <summary>
        /// Parses an integer
        /// </summary>
        /// <param name="state"></param>
        /// <returns>Parsed integer</returns>
        private object parseI()
        {
            parseCHR('i');
            parseCOL();
            return _parseNUM();
        }

        /// <summary>
        /// Parses a string
        /// </summary>
        /// <returns>Parsed string</returns>
        private object parseS()
        {
            parseCHR('s');
            parseCOL();

            var length = _parseNUM();

            parseCOL();
            parseQTE();

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

            parseQTE();
            return str;
        }

        /// <summary>
        /// Parses an array
        /// </summary>
        /// <returns></returns>
        private object parseA()
        {
            parseCHR('a');
            parseCOL();

            var size = _parseNUM();

            parseCOL();
            parseOBR();

            var dictionary = new Dictionary<object, object>();
            for (int i = 0; i < size; i++)
            {
                var key = parse();
                parseSEM();
                var value = parse();
                parseSEM(i != size - 1);
                dictionary[key] = value;
            }

            parseCBR();
            return dictionary;
        }

        private object parse()
        {
            switch (Current)
            {
                case 'i': return parseI();
                case 's': return parseS();
                case 'a': return parseA();
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
            return (IDictionary<object, object>)parseA();
        }
    }
}