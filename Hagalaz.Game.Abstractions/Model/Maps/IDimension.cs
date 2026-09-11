namespace Hagalaz.Game.Abstractions.Model.Maps
{
    /// <summary>
    /// Defines the contract for a dimension, which is a separate instance of the game world.
    /// The global world dimension ID is always 0.
    /// </summary>
    public interface IDimension
    {
        /// <summary>
        /// Gets the unique identifier for this dimension.
        /// </summary>
        public int Id { get; }

    }
}
