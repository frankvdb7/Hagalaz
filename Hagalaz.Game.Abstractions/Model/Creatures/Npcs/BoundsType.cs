namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
{
    /// <summary>
    /// The type of walking the npc does.
    /// </summary>
    public enum BoundsType : int
    {
        /// <summary>
        /// The npc does not walk, it stays in the same location.
        /// </summary>
        Static,
        /// <summary>
        /// The npc walks in a specified range.
        /// </summary>
        Range,
        /// <summary>
        /// The npc is free to roam where ever.
        /// </summary>
        Roam
    }
}
