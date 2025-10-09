namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// Defines the contract for a Non-Player Character (NPC) type, containing all the
    /// static data for a specific kind of NPC as loaded from the game cache.
    /// </summary>
    public interface INpcType : IType
    {
        /// <summary>
        /// Gets the unique identifier for this NPC type.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets or sets the combat level of the NPC. This value may be adjusted after being loaded from the cache.
        /// </summary>
        int CombatLevel { get; set; }

        /// <summary>
        /// Gets the name of the NPC as it appears in-game.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the size of the NPC in game tiles (e.g., a value of 1 means a 1x1 tile footprint).
        /// </summary>
        int Size { get; }

        /// <summary>
        /// Gets the default direction the NPC faces when it spawns.
        /// </summary>
        sbyte SpawnFaceDirection { get; }

        /// <summary>
        /// Gets the animation ID used for the NPC's default standing or idle animation.
        /// </summary>
        int RenderId { get; }
    }
}
