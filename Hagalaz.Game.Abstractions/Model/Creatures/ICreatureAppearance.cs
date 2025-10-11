namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// Defines the contract for managing a creature's visual appearance.
    /// </summary>
    public interface ICreatureAppearance
    {
        /// <summary>
        /// Gets or sets a value indicating whether the creature is currently visible.
        /// </summary>
        bool Visible { get; set; }

        /// <summary>
        /// Gets the size of the creature in game tiles.
        /// </summary>
        int Size { get; }
    }
}