using System;

namespace Hagalaz.Game.Extensions
{
    /// <summary>
    /// Provides extension methods for the <see cref="Random"/> class.
    /// </summary>
    public static class RandomExtensions
    {
        /// <summary>
        /// Returns a random floating-point number that is greater than or equal to 0.0, and less than the specified maximum.
        /// </summary>
        /// <param name="random">The random number generator.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated.</param>
        /// <returns>A double-precision floating-point number that is greater than or equal to 0.0 and less than <paramref name="maxValue"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than or equal to zero.</exception>
        public static double Next(this Random random, double maxValue)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxValue);
            return random.NextDouble() * maxValue;
        }
    }
}