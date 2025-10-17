namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
{
    /// <summary>
    /// Defines the contract for an NPC's movement boundaries, which restrict its wandering area.
    /// </summary>
    public interface IBounds
    {
        /// <summary>
        /// Gets the type of movement boundary (e.g., Static, Range, Roam).
        /// </summary>
        BoundsType BoundsType { get; }

        /// <summary>
        /// Gets the NPC's default spawn location.
        /// </summary>
        ILocation DefaultLocation { get; }

        /// <summary>
        /// Gets the minimum coordinate (bottom-left corner) of the boundary area.
        /// </summary>
        ILocation MinimumLocation { get; }

        /// <summary>
        /// Gets the maximum coordinate (top-right corner) of the boundary area.
        /// </summary>
        ILocation MaximumLocation { get; }

        /// <summary>
        /// Checks if a specific location is within the NPC's movement boundaries.
        /// </summary>
        /// <param name="location">The location to check.</param>
        /// <returns><c>true</c> if the location is within the defined bounds; otherwise, <c>false</c>.</returns>
        bool InBounds(ILocation location);
    }
}