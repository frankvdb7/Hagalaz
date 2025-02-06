using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Hagalaz.Utilities
{
    /// <summary>
    /// Provides multiple ways of working with strings.
    /// </summary>
    public static class StringUtilities
    {
        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public delegate T ValueParser<out T>(string value);

        /// <summary>
        /// The valid mail
        /// </summary>
        private static readonly Regex _validMail = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$", RegexOptions.IgnoreCase);

        /// <summary>
        /// The valid name
        /// </summary>
        private static readonly Regex _validName = new Regex(@"(^[A-Za-z0-9]{1,12}$)|(^[A-Za-z0-9]+[\-\s][A-Za-z0-9]+[\-\s]{0,1}[A-Za-z0-9]+$)");

        /// <summary>
        /// Valid characters able to be sent to client.
        /// </summary>
        private static readonly char[] _validChars =
        [
            '_', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i',
            'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's',
            't', 'u', 'v', 'w', 'x', 'y', 'z', '0', '1', '2',
            '3', '4', '5', '6', '7', '8', '9'
        ];

        /// <summary>
		/// Determines whether [is valid email] [the specified email].
		/// </summary>
		/// <param name="email">The email.</param>
		/// <returns>
		///   <c>true</c> if [is valid email] [the specified email]; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsValidEmail(string email) => _validMail.IsMatch(email);

        /// <summary>
        /// Determines whether [is valid name] [the specified name].
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>
        ///   <c>true</c> if [is valid name] [the specified name]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsValidName(string name)
        {
            if (name.Length < 1 || name.Length > 12)
                return false;
            return _validName.Match(name).Success;
        }

        /// <summary>
        /// Encodes the values.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values">The values.</param>
        /// <returns></returns>
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
        /// Encodes the values.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <returns></returns>
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
        /// Decodes the values.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data">The data.</param>
        /// <param name="parser">The parser.</param>
        /// <param name="separator"></param>
        /// <returns></returns>
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
        /// Decodes the values.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public static bool[] DecodeValues(string data)
        {
            var split = data.Split(',');
            var values = new bool[split.Length];
            for (int i = 0; i < split.Length; i++)
                values[i] = int.Parse(split[i]) == 1;
            return values;
        }

        /// <summary>
        /// Converts a given 64-bit integer to a string.
        /// </summary>
        /// <param name="value">The value of the integer.</param>
        /// <returns>Returns a string.</returns>
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
        /// Converts a given string to a 64-bit integer.
        /// </summary>
        /// <param name="s">The string to convert.</param>
        /// <returns>Returns a 64-bit integer.</returns>
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
        /// Gets a string inbetween a string.
        /// </summary>
        /// <param name="strBegin">The beginning marker.</param>
        /// <param name="strEnd">The end marker.</param>
        /// <param name="strSource">The source.</param>
        /// <param name="includeBegin">Whether to include begin marker.</param>
        /// <param name="includeEnd">Whtehr to include end marker.</param>
        /// <returns>System.String[][].</returns>
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
        /// Formats the number.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string FormatNumber(int value) => value.ToString("#,###,##0");
    }
}
