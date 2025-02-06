using System.Linq;

namespace Hagalaz.Game.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Format's user typed message for packet. ( Right now it replaces &gt; and &lt;
        /// with &lt;gt&gt; and &lt;lt&gt; )
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>System.String.</returns>
        public static string FormatUserMessageForPacket(this string text) => text.Replace("<", "<lt>").Replace(">", "<gt>");
        
        /// <summary>
        /// Strings to int.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <returns></returns>
        public static int StringToInt(this string s) => s.Aggregate(0, (current, t) => (current << 5) - current + t);
    }
}