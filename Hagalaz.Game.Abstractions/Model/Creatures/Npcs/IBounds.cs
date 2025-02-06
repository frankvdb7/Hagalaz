namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
{
    /// <summary>
    /// 
    /// </summary>
    public interface IBounds
    {
        /// <summary>
        /// Contains npc bounds type.
        /// </summary>
        /// <value>The type of the bounds.</value>
        BoundsType BoundsType { get; }
        /// <summary>
        /// The NPC's default spawn location.
        /// </summary>
        /// <value>The default location.</value>
        ILocation DefaultLocation { get; }
        /// <summary>
        /// The NPC's minimum location.
        /// </summary>
        /// <value>The minimum location.</value>
        ILocation MinimumLocation { get; }
        /// <summary>
        /// The NPC's maximum location.
        /// </summary>
        /// <value>The maximum location.</value>
        ILocation MaximumLocation { get; }
        /// <summary>
        /// Get's if specific location is in npc location bounds.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool InBounds(ILocation location);
    }
}
