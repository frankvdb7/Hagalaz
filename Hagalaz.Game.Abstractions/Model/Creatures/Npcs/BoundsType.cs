namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
{
    /// <summary>
    /// Defines the different types of movement boundaries for an NPC, controlling how it wanders.
    /// </summary>
    public enum BoundsType : int
    {
        /// <summary>
        /// The NPC remains stationary at its spawn point and does not walk.
        /// </summary>
        Static,
        /// <summary>
        /// The NPC walks randomly within a specified rectangular area.
        /// </summary>
        Range,
        /// <summary>
        /// The NPC is free to roam anywhere on the map without restrictions.
        /// </summary>
        Roam
    }
}