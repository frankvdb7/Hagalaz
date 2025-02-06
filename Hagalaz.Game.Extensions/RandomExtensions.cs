using System;

namespace Hagalaz.Game.Extensions
{
    public static class RandomExtensions
    {
        /// <summary>
        /// Retrieves the next random value from the random number generator.
        /// The result is always between 0.0 and the given max-value (excluding).
        /// </summary>
        /// <param name="random"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static double Next(this Random random, double maxValue)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxValue);
            return random.NextDouble() * maxValue;
        }
    }
}