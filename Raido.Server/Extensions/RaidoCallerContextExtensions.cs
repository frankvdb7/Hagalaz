using System.Diagnostics.CodeAnalysis;

namespace Raido.Server.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="RaidoCallerContext"/>.
    /// </summary>
    public static class RaidoCallerContextExtensions
    {
        /// <summary>
        /// Tries to get a value from the context items.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="context">The Raido caller context.</param>
        /// <param name="key">The key of the item to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value"/> parameter.</param>
        /// <returns><c>true</c> if the context items contains an element with the specified key; otherwise, <c>false</c>.</returns>
        public static bool TryGetItem<TValue>(this RaidoCallerContext context, object key, [NotNullWhen(true)] out TValue? value)
        {
            if (context.Items.TryGetValue(key, out var obj) && obj is TValue val)
            {
                value = val;
                return true;
            }

            value = default;
            return false;
        }
    }
}