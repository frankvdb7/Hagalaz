namespace Hagalaz.Game.Abstractions.Logic.Random
{
    /// <summary>
    /// Defines a contract for a random number provider, abstracting the underlying implementation
    /// to allow for deterministic or different random number generation strategies.
    /// </summary>
    public interface IRandomProvider
    {
        /// <summary>
        /// Returns a non-negative random integer that is less than the specified maximum.
        /// </summary>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated.</param>
        /// <returns>A 32-bit signed integer greater than or equal to 0, and less than <paramref name="maxValue"/>.</returns>
        int Next(int maxValue);

        /// <summary>
        /// Returns a random integer that is within a specified range.
        /// </summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number returned.</param>
        /// <returns>A 32-bit signed integer greater than or equal to <paramref name="minValue"/> and less than <paramref name="maxValue"/>.</returns>
        int Next(int minValue, int maxValue);

        /// <summary>
        /// Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
        /// </summary>
        /// <returns>A double-precision floating point number.</returns>
        double NextDouble();

        /// <summary>
        /// Returns a random floating-point number that is greater than or equal to 0.0, and less than the specified maximum.
        /// </summary>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated.</param>
        /// <returns>A double-precision floating point number.</returns>
        double Next(double maxValue);
    }
}
