using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Hagalaz.Utilities
{
    /// <summary>
    /// Provides a collection of static utility methods for string manipulation, validation, and conversion.
    /// </summary>
    public static class StringUtilities
    {
        /// <summary>
        /// Defines a delegate for parsing a string into a specific type.
        /// </summary>
        /// <typeparam name="T">The target type of the parsing operation.</typeparam>
        /// <param name="value">The string value to parse.</param>
        /// <returns>The parsed value of type <typeparamref name="T"/>.</returns>
        public delegate T ValueParser<out T>(string value);

        /// <summary>
        /// A regular expression for validating standard email address formats.
        /// </summary>
        private static readonly Regex _validMail = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$", RegexOptions.IgnoreCase);

        /// <summary>
        /// A regular expression for validating names, likely usernames, allowing for simple or multi-part names.
        /// </summary>
        private static readonly Regex _validName = new Regex(@"(^[A-Za-z0-9]{1,12}$)|(^[A-Za-z0-9]+[\-\s][A-Za-z0-9]+[\-\s]{0,1}[A-Za-z0-9]+$)");

        /// <summary>
        /// An array of characters that are considered valid for specific contexts, such as client-side communication or base-37 encoding.
        /// </summary>
        private static readonly char[] _validChars =
        [
            '_', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i',
            'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's',
            't', 'u', 'v', 'w', 'x', 'y', 'z', '0', '1', '2',
            '3', '4', '5', '6', '7', '8', '9'
        ];

        /// <summary>
        /// Validates whether the given string is a well-formed email address.
        /// </summary>
        /// <param name="email">The email string to validate.</param>
        /// <returns><c>true</c> if the email format is valid; otherwise, <c>false</c>.</returns>
		public static bool IsValidEmail(string email) => _validMail.IsMatch(email);

        /// <summary>
        /// Validates whether the given string is a valid name, conforming to length and character constraints.
        /// </summary>
        /// <param name="name">The name string to validate.</param>
        /// <returns><c>true</c> if the name is valid; otherwise, <c>false</c>.</returns>
        public static bool IsValidName(string name)
        {
            if (name.Length < 1 || name.Length > 12)
                return false;
            return _validName.Match(name).Success;
        }

        /// <summary>
        /// Encodes an array of values into a single comma-separated string.
        /// </summary>
        /// <typeparam name="T">The type of values in the array.</typeparam>
        /// <param name="values">The array of values to encode.</param>
        /// <returns>A string containing the comma-separated values.</returns>
        public static string EncodeValues<T>(T[] values)
        {
            StringBuilder bld = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                bld.Append(values[i]);
                if ((i + 1) < values.Length)
                    bld.Append(",");
            }
            return bld.ToString();
        }

        /// <summary>
        /// Encodes an array of booleans into a comma-separated string, where <c>true</c> is "1" and <c>false</c> is "0".
        /// </summary>
        /// <param name="values">The array of booleans to encode.</param>
        /// <returns>A string containing the comma-separated numeric representation of the booleans.</returns>
        public static string EncodeValues(bool[] values)
        {
            var bld = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                bld.Append(values[i] ? 1 : 0);
                if ((i + 1) < values.Length)
                    bld.Append(",");
            }
            return bld.ToString();
        }

        /// <summary>
        /// Parses a comma-separated string into an enumerable of doubles. Invalid entries default to 0.0.
        /// </summary>
        /// <param name="input">The comma-separated string of numbers.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of doubles.</returns>
        public static IEnumerable<double> SelectDoubleFromString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                yield break; // Return an empty enumerable for an empty or null input.
            }

            foreach (var str in input.Split(','))
            {
                if (double.TryParse(str, out double parsedValue))
                {
                    yield return parsedValue;
                }
                else
                {
                    // Handle the case where parsing fails (e.g., set a default value).
                    yield return 0.0;
                }
            }
        }

        /// <summary>
        /// Parses a comma-separated string into an enumerable of integers. Invalid entries default to 0.
        /// </summary>
        /// <param name="input">The comma-separated string of numbers.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of integers.</returns>
        public static IEnumerable<int> SelectIntFromString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                yield break; // Return an empty enumerable for an empty or null input.
            }

            foreach (var str in input.Split(','))
            {
                if (int.TryParse(str, out int parsedValue))
                {
                    yield return parsedValue;
                }
                else
                {
                    // Handle the case where parsing fails (e.g., set a default value).
                    yield return 0;
                }
            }
        }

        /// <summary>
        /// Parses a comma-separated string of numbers into an enumerable of booleans, where "1" is <c>true</c>.
        /// </summary>
        /// <param name="input">The comma-separated string of numbers (e.g., "1,0,1").</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of booleans.</returns>
        public static IEnumerable<bool> SelectBoolFromString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                yield break; // Return an empty enumerable for an empty or null input.
            }

            foreach (var number in SelectIntFromString(input))
            {
                yield return number == 1;
            }
        }

        /// <summary>
        /// Decodes a separated string into an array of a specified type using a custom parser.
        /// </summary>
        /// <typeparam name="T">The target type for the decoded values.</typeparam>
        /// <param name="data">The string data to decode.</param>
        /// <param name="parser">The delegate function used to parse each string segment.</param>
        /// <param name="separator">The character used to separate values in the string. Defaults to a comma.</param>
        /// <returns>An array of type <typeparamref name="T"/> containing the decoded values.</returns>
        public static T[] DecodeValues<T>(string data, ValueParser<T> parser, char separator = ',')
        {
            if (string.IsNullOrEmpty(data))
                return [];
            var split = data.Split(separator);
            T[] values = new T[split.Length];
            for (var i = 0; i < split.Length; i++)
                values[i] = parser.Invoke(split[i]);
            return values;
        }

        /// <summary>
        /// Decodes a comma-separated string of numbers into a boolean array, where "1" represents <c>true</c>.
        /// </summary>
        /// <param name="data">The comma-separated string to decode.</param>
        /// <returns>A boolean array representing the decoded data.</returns>
        public static bool[] DecodeValues(string data)
        {
            var split = data.Split(',');
            var values = new bool[split.Length];
            for (int i = 0; i < split.Length; i++)
                values[i] = int.Parse(split[i]) == 1;
            return values;
        }

        /// <summary>
        /// Converts a 64-bit integer into a base-37 encoded string, commonly used for names or identifiers.
        /// </summary>
        /// <param name="value">The integer value to convert.</param>
        /// <returns>A base-37 encoded string, or <c>null</c> if the value is out of the valid range.</returns>
        public static string? LongToString(this long value)
        {
            if (value <= 0L || value >= 0x5B5B57F8A98A5DD1L)
                return null;
            if (value % 37L == 0L)
                return null;

            int i = 0;
            var ac = new char[12];
            while (value != 0L)
            {
                long l1 = value;
                value /= 37L;
                ac[11 - i++] = _validChars[(int)(l1 - value * 37L)];
            }
            return new string(ac, 12 - i, i);
        }

        /// <summary>
        /// Converts a base-37 encoded string back into a 64-bit integer.
        /// </summary>
        /// <param name="s">The base-37 encoded string to convert.</param>
        /// <returns>The decoded 64-bit integer value.</returns>
        public static long StringToLong(this string s)
        {
            long l = 0L;
            for (int i = 0; i < s.Length && i < 12; i++)
            {
                char c = s[i];
                l *= 37L;

                if (c >= 'A' && c <= 'Z')
                    l += (1 + c) - 65;
                else if (c >= 'a' && c <= 'z')
                    l += (1 + c) - 97;
                else if (c >= '0' && c <= '9')
                    l += (27 + c) - 48;
            }
            while (l % 37L == 0L && l != 0L) l /= 37L;
            return l;
        }

        /// <summary>
        /// Extracts a substring between two specified markers.
        /// </summary>
        /// <param name="strBegin">The string that marks the beginning of the substring.</param>
        /// <param name="strEnd">The string that marks the end of the substring.</param>
        /// <param name="strSource">The source string to search within.</param>
        /// <param name="includeBegin">If true, the beginning marker is included in the result.</param>
        /// <param name="includeEnd">If true, the ending marker is included in the result.</param>
        /// <returns>A string array where the first element is the extracted substring and the second element is the remainder of the source string.</returns>
        public static string[] GetStringInBetween(string strBegin, string strEnd, string strSource, bool includeBegin, bool includeEnd)
        {
            string[] result = { "", "" };
            int iIndexOfBegin = strSource.IndexOf(strBegin, StringComparison.Ordinal);
            if (iIndexOfBegin != -1)
            {
                // include the Begin string if desired
                if (includeBegin)
                    iIndexOfBegin -= strBegin.Length;
                strSource = strSource.Substring(iIndexOfBegin
                    + strBegin.Length);
                int iEnd = strSource.IndexOf(strEnd, StringComparison.Ordinal);
                if (iEnd != -1)
                {
                    // include the End string if desired
                    if (includeEnd)
                        iEnd += strEnd.Length;
                    result[0] = strSource.Substring(0, iEnd);
                    // advance beyond this segment
                    if (iEnd + strEnd.Length < strSource.Length)
                        result[1] = strSource.Substring(iEnd
                            + strEnd.Length);
                }
            }
            else
                // stay where we are
                result[1] = strSource;
            return result;
        }

        /// <summary>
        /// Formats an integer as a string with thousands separators.
        /// </summary>
        /// <param name="value">The integer value to format.</param>
        /// <returns>A formatted string representation of the number (e.g., "1,234,567").</returns>
        public static string FormatNumber(int value) => value.ToString("#,###,##0");
    }
}
