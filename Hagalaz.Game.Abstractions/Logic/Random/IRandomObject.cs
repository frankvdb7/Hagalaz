namespace Hagalaz.Game.Abstractions.Logic.Random
{
    /// <summary>
    /// Defines a contract for an object that can be selected from a weighted random table.
    /// </summary>
    public interface IRandomObject
    {
        /// <summary>
        /// Gets a value indicating whether this object will always be selected, regardless of its probability.
        /// </summary>
        bool Always { get; }

        /// <summary>
        /// Gets a value indicating whether this object is currently enabled and can be selected.
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// Gets the probability weight of this object being selected. Higher values are more likely.
        /// </summary>
        double Probability { get; }
    }
}
