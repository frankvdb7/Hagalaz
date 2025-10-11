namespace Hagalaz.Game.Abstractions.Logic.Random
{
    /// <summary>
    /// Represents the context for a random object selection, containing the object and its probability details.
    /// This context is passed to modifiers, allowing them to dynamically alter the selection probability.
    /// </summary>
    /// <param name="Object">The random object being processed.</param>
    public record RandomObjectContext(IRandomObject Object)
    {
        /// <summary>
        /// Gets the base probability of the object as defined in its table.
        /// </summary>
        public double BaseProbability { get; init; } = Object.Probability;

        /// <summary>
        /// Gets or sets the modified probability of the object, which can be altered by modifiers.
        /// </summary>
        public double ModifiedProbability { get; set; } = Object.Probability;
    }
}