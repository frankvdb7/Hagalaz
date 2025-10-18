namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
{
    /// <summary>
    /// Defines how an NPC reacts when a character enters its line of sight.
    /// </summary>
    public enum ReactionType : int
    {
        /// <summary>
        /// The NPC is passive and will not initiate combat.
        /// </summary>
        Passive = 0,
        /// <summary>
        /// The NPC is aggressive and will attack any character on sight.
        /// </summary>
        Aggressive = 1,
        /// <summary>
        /// The NPC is aggressive only towards characters whose combat level is within a certain range of its own.
        /// </summary>
        CombatAggressive = 2
    }
}