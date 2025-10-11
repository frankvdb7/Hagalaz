namespace Hagalaz.Game.Abstractions.Logic.Random
{
    /// <summary>
    /// Defines a contract for a modifier that can alter the properties of a random object within a given context.
    /// This allows for dynamic changes to loot or other random selections based on game state (e.g., player stats, world events).
    /// </summary>
    public interface IRandomObjectModifier
    {
        /// <summary>
        /// Applies a modification to the random object within the provided context.
        /// </summary>
        /// <param name="context">The context of the random object selection, containing the object to be modified and other relevant data.</param>
        public void Apply(RandomObjectContext context);
    }
}