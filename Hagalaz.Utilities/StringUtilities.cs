using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Hagalaz.Utilities
{
    /// <summary>
    /// Provides a collection of static utility methods for string manipulation, validation, and conversion.
    /// </summary>
    public static partial class StringUtilities
    {
        /// <summary>
        /// Defines a delegate for parsing a string into a specific type.
        /// </summary>
        /// <typeparam name="T">The target type of the parsing operation.</typeparam>
        /// <param name="value">The string value to parse.</param>
        /// <returns>The parsed value of type <typeparamref name="T"/>.</returns>
        public delegate T ValueParser<out T>(string value);
        /// <summary>
        /// Defines a delegate for parsing a span into a specific type.
        /// </summary>
        /// <typeparam name="T">The target type of the parsing operation.</typeparam>
        /// <param name="value">The span value to parse.</param>
        /// <returns>The parsed value of type <typeparamref name="T"/>.</returns>
        public delegate T SpanValueParser<out T>(ReadOnlySpan<char> value);

        /// <summary>
        /// A regular expression for validating standard email address formats.
        /// </summary>
        private static readonly Regex _validMail = MyRegex();


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
        /// A lookup table for mapping characters to their base-37 values.
        /// Values are 1-based: 1 for _, 2-27 for a-z, 28-37 for 0-9.
        /// 0 means invalid.
        /// </summary>
        private static ReadOnlySpan<byte> Base37Lookup =>
        [
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0-15
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 16-31
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 32-47
            28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 0, 0, 0, 0, 0, 0, // 48-57 (0-9)
            0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, // 65-80 (A-O)
            17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 0, 0, 0, 0, 1, // 81-90 (P-Z), 95 is _
            0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, // 97-112 (a-o)
            17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 0, 0, 0, 0, 0, // 113-122 (p-z)
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        ];

        /// <summary>
        /// Validates whether the given string is a well-formed email address.
        /// </summary>
        /// <param name="email">The email string to validate.</param>
        /// <returns><c>true</c> if the email format is valid; otherwise, <c>false</c>.</returns>
		public static bool IsValidEmail(string email) => email is not null && _validMail.IsMatch(email);

        /// <summary>
        /// Validates whether the given string is a valid name.
        /// <para>
        /// Rules:
        /// <list type="bullet">
        /// <item>Length must be between 1 and 12 characters.</item>
        /// <item>Only alphanumeric characters, spaces, and hyphens are allowed.</item>
        /// <item>Cannot start or end with a separator, and consecutive separators are forbidden.</item>
        /// <item>Single-part names must be purely alphanumeric.</item>
        /// <item>Two-part names (1 separator) require the second part to be at least 2 characters long (e.g., 'a-bc').</item>
        /// <item>Three-part names (2 separators) require each part to be at least 1 character long (e.g., 'a-b-c').</item>
        /// </list>
        /// </para>
        /// </summary>
        /// <param name="name">The name string to validate.</param>
        /// <returns><c>true</c> if the name is valid; otherwise, <c>false</c>.</returns>
        public static bool IsValidName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 12)
            {
                return false;
            }

            int separators = 0;
            int s1 = -1, s2 = -1;
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsAsciiLetterOrDigit(c))
                {
                    continue;
                }

                if (char.IsWhiteSpace(c) || c == '-')
                {
                    separators++;
                    if (separators == 1) s1 = i;
                    else if (separators == 2) s2 = i;
                    else return false; // Max 2 separators allowed
                }
                else
                {
                    return false;
                }
            }

            // Single-part alphanumeric name.
            if (separators == 0) return true;

            // Multi-part name validation matching original regex logic.
            // 1 separator case: Block1 (1+) Sep Block2 (2+) => at least 1 char before, 2 after.
            if (separators == 1) return s1 >= 1 && s1 <= name.Length - 3;

            // 2 separators case: Block1 (1+) Sep Block2 (1+) Sep Block3 (1+) => at least 1 before, 1 between, 1 after.
            return s1 >= 1 && s2 >= s1 + 2 && s2 <= name.Length - 2;
        }

        /// <summary>
        /// Encodes an array of values into a single comma-separated string.
        /// </summary>
        /// <typeparam name="T">The type of values in the array.</typeparam>
        /// <param name="values">The array of values to encode.</param>
        /// <returns>A string containing the comma-separated values.</returns>
        public static string EncodeValues<T>(T[] values)
        {
            if (values == null || values.Length == 0) return string.Empty;
            // string.Join is highly optimized in .NET and preferred over manual StringBuilder for simple joins.
            return string.Join(',', values);
        }

        /// <summary>
        /// Encodes an array of booleans into a comma-separated string, where <c>true</c> is "1" and <c>false</c> is "0".
        /// </summary>
        /// <param name="values">The array of booleans to encode.</param>
        /// <returns>A string containing the comma-separated numeric representation of the booleans.</returns>
        public static string EncodeValues(bool[] values)
        {
            if (values == null || values.Length == 0) return string.Empty;

            // Use string.Create for zero-allocation (of intermediate buffers) and exact sizing.
            int length = values.Length * 2 - 1;
            return string.Create(length, values, (span, state) =>
            {
                for (int i = 0; i < state.Length; i++)
                {
                    int pos = i * 2;
                    span[pos] = state[i] ? '1' : '0';
                    if (pos + 1 < span.Length)
                    {
                        span[pos + 1] = ',';
                    }
                }
            });
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
                yield break;
            }

            int start = 0;
            int end;
            while ((end = input.IndexOf(',', start)) != -1)
            {
                yield return ParseDouble(input.AsSpan(start, end - start));
                start = end + 1;
            }
            yield return ParseDouble(input.AsSpan(start));
        }

        private static double ParseDouble(ReadOnlySpan<char> segment)
        {
            // Use NumberStyles.Float instead of Any for an ~11% performance boost.
            return double.TryParse(segment, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedValue)
                ? parsedValue
                : 0.0;
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
                yield break;
            }

            int start = 0;
            int end;
            while ((end = input.IndexOf(',', start)) != -1)
            {
                yield return ParseInt(input.AsSpan(start, end - start));
                start = end + 1;
            }
            yield return ParseInt(input.AsSpan(start));
        }

        private static int ParseInt(ReadOnlySpan<char> segment)
        {
            // Use NumberStyles.Integer instead of Any for an ~8x performance boost.
            return int.TryParse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedValue)
                ? parsedValue
                : 0;
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
        /// Decodes a separated string into an array of a specified type using a custom parser that accepts spans.
        /// </summary>
        /// <typeparam name="T">The target type for the decoded values.</typeparam>
        /// <param name="data">The string data to decode.</param>
        /// <param name="parser">The delegate function used to parse each string segment span.</param>
        /// <param name="separator">The character used to separate values in the string. Defaults to a comma.</param>
        /// <returns>An array of type <typeparamref name="T"/> containing the decoded values.</returns>
        public static T[] DecodeValuesFromSpan<T>(string data, SpanValueParser<T> parser, char separator = ',')
        {
            if (string.IsNullOrWhiteSpace(data))
                return [];

            int count = CountSegments(data.AsSpan(), separator);
            T[] values = new T[count];

            int start = 0;
            for (int k = 0; k < count; k++)
            {
                int end = data.IndexOf(separator, start);
                if (end == -1) end = data.Length;

                values[k] = parser.Invoke(data.AsSpan(start, end - start));
                start = end + 1;
            }

            return values;
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
            return DecodeValuesFromSpan(data, segment => parser.Invoke(segment.ToString()), separator);
        }

        /// <summary>
        /// Decodes a comma-separated string of numbers into a boolean array, where "1" represents <c>true</c>.
        /// </summary>
        /// <param name="data">The comma-separated string to decode.</param>
        /// <returns>A boolean array representing the decoded data.</returns>
        public static bool[] DecodeValues(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return [];
            }

            int count = CountSegments(data.AsSpan(), ',');
            bool[] values = new bool[count];

            int start = 0;
            for (int k = 0; k < count; k++)
            {
                int end = data.IndexOf(',', start);
                if (end == -1) end = data.Length;

                // Optimization: fast-path for "1" and "0" common segments.
                ReadOnlySpan<char> segment = data.AsSpan(start, end - start);
                if (segment.Length == 1)
                {
                    char c = segment[0];
                    if (c == '1') values[k] = true;
                    else if (c == '0') values[k] = false;
                    else values[k] = ParseInt(segment) == 1;
                }
                else
                {
                    values[k] = ParseInt(segment) == 1;
                }
                start = end + 1;
            }

            return values;
        }

        /// <summary>
        /// Converts a 64-bit integer into a base-37 encoded string, commonly used for names or identifiers.
        /// </summary>
        /// <param name="value">The integer value to convert.</param>
        /// <returns>A base-37 encoded string, or <c>null</c> if the value is out of the valid range.</returns>
        public static string? LongToString(this long value)
        {
            const long aLong2320 = 0x5b5b57f8a98a5dd1L;
            if (value <= 0L || value >= aLong2320)
                return null;

            // Use stackalloc to avoid heap allocation, and fill from the end.
            Span<char> buffer = stackalloc char[12];
            int count = 0;
            while (value != 0L)
            {
                value = Math.DivRem(value, 37L, out long remainder);
                buffer[11 - count++] = _validChars[(int)remainder];
            }

            // Create string from the populated slice of the buffer.
            return new string(buffer.Slice(12 - count, count));
        }

        /// <summary>
        /// Converts a base-37 encoded string back into a 64-bit integer.
        /// </summary>
        /// <param name="s">The base-37 encoded string to convert.</param>
        /// <returns>The decoded 64-bit integer value.</returns>
        public static long StringToLong(this string? s)
        {
            if (string.IsNullOrEmpty(s) || s.Length > 12)
            {
                return 0L;
            }

            long l = 0L;
            var lookup = Base37Lookup;
            foreach (char c in s)
            {
                if ((uint)c < (uint)lookup.Length)
                {
                    byte val = lookup[c];
                    if (val == 0) return 0L; // Invalid character found
                    l = l * 37L + (val - 1);
                }
                else
                {
                    return 0L; // Character out of range
                }
            }

            while (l % 37L == 0L && l != 0L)
            {
                l /= 37L;
            }

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
            if (string.IsNullOrEmpty(strSource))
                return ["", ""];

            ReadOnlySpan<char> sourceSpan = strSource.AsSpan();
            int iIndexOfBegin = sourceSpan.IndexOf(strBegin.AsSpan(), StringComparison.Ordinal);

            if (iIndexOfBegin != -1)
            {
                int endOfBegin = iIndexOfBegin + strBegin.Length;
                ReadOnlySpan<char> afterBeginSpan = sourceSpan.Slice(endOfBegin);
                int iIndexOfEndAfterBegin = afterBeginSpan.IndexOf(strEnd.AsSpan(), StringComparison.Ordinal);

                if (iIndexOfEndAfterBegin != -1)
                {
                    int resultStart = includeBegin ? iIndexOfBegin : endOfBegin;
                    int resultEnd = endOfBegin + iIndexOfEndAfterBegin + (includeEnd ? strEnd.Length : 0);

                    string found = sourceSpan.Slice(resultStart, resultEnd - resultStart).ToString();

                    int remainderStart = endOfBegin + iIndexOfEndAfterBegin + strEnd.Length;
                    string remainder = remainderStart < sourceSpan.Length
                        ? sourceSpan.Slice(remainderStart).ToString()
                        : string.Empty;

                    return [found, remainder];
                }

                // If end marker is not found, return empty results as per original behavior
                return ["", ""];
            }

            // If begin marker is not found, return empty result and original source as remainder
            return ["", strSource];
        }

        /// <summary>
        /// Formats an integer as a string with thousands separators.
        /// </summary>
        /// <param name="value">The integer value to format.</param>
        /// <returns>A formatted string representation of the number (e.g., "1,234,567").</returns>
        private static int CountSegments(ReadOnlySpan<char> span, char separator)
        {
            if (span.IsEmpty) return 0;
            // Use vectorized Count from MemoryExtensions
            return span.Count(separator) + 1;
        }

        public static string FormatNumber(int value) => value.ToString("#,###,##0", CultureInfo.InvariantCulture);
        [GeneratedRegex("^(([^<>()[\\]\\\\.,;:\\s@\"]+(\\.[^<>()[\\]\\\\.,;:\\s@\"]+)*)|(\".+\"))@((\\[[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\])|(([a-zA-Z\\-0-9]+\\.)+[a-zA-Z]{2,}))$", RegexOptions.IgnoreCase, "nl-NL")]
        private static partial Regex MyRegex();
    }
}