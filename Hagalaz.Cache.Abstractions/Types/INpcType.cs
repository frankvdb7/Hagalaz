namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// 
    /// </summary>
    public interface INpcType : IType
    {
        /// <summary>
        /// Contains the NPC's id.
        /// </summary>
        int Id { get; }
        /// <summary>
        /// Gets the combat level.
        /// </summary>
        /// <value>The combat level.</value>
        int CombatLevel { get; set; }
        /// <summary>
        /// The NPC's name.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Contains size of the npc.
        /// </summary>
        int Size { get; }
        /// <summary>
        /// Contains the standart spawn direction. (As loaded from owner)
        /// </summary>
        sbyte SpawnFaceDirection { get; }
        /// <summary>
        /// Gets the render emote.
        /// </summary>
        /// <value>The render emote.</value>
        int RenderId { get; }
    }
}
