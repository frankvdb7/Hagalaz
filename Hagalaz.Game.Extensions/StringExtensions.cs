using System.Linq;

namespace Hagalaz.Game.Extensions
{
    /// <summary>
    /// Provides extension methods for the <see cref="string"/> class.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Formats a user-typed message for network packets by escaping special characters.
        /// Currently, it replaces '&lt;' with '&lt;lt&gt;' and '&gt;' with '&lt;gt&gt;'.
        /// </summary>
        /// <param name="text">The raw string to format.</param>
        /// <returns>A formatted string that is safe to be sent in a network packet.</returns>
        public static string FormatUserMessageForPacket(this string text) => text.Replace("<", "<lt>").Replace(">", "<gt>");
        
        /// <summary>
        /// Converts a string to an integer using a custom hashing algorithm.
        /// </summary>
        /// <param name="s">The string to convert.</param>
        /// <returns>An integer hash of the input string.</returns>
        public static int StringToInt(this string s) => s.Aggregate(0, (current, t) => (current << 5) - current + t);
    }
}