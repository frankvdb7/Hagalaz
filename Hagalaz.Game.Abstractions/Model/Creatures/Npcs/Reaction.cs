namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
{
    /// <summary>
    /// A npc's reaction to seeing characters.
    /// </summary>
    public enum ReactionType : int
    {
        /// <summary>
        /// The npc is passive, and will not attack first or is not attackable.
        /// </summary>
        Passive = 0,
        /// <summary>
        /// The npc is aggressive, and will attack first.
        /// </summary>
        Aggressive = 1,
        /// <summary>
        /// The npc is aggressive when the player is within a certain combat range, and will attack first.
        /// </summary>
        CombatAggressive = 2
    }
}
