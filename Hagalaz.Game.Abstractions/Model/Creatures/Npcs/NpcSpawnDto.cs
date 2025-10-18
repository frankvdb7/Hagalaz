namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
{
    /// <summary>
    /// A data transfer object containing information required to spawn an NPC.
    /// </summary>
    public record NpcSpawnDto
    {
        /// <summary>
        /// Gets the ID of the NPC to spawn.
        /// </summary>
        public int NpcId { get; init; }

        /// <summary>
        /// Gets or sets the spawn location of the NPC.
        /// </summary>
        public ILocation Location { get; set; } = default!;

        /// <summary>
        /// Gets or sets the minimum coordinate (bottom-left corner) of the NPC's movement boundary.
        /// </summary>
        public ILocation MinimumBounds { get; set; } = default!;

        /// <summary>
        /// Gets or sets the maximum coordinate (top-right corner) of the NPC's movement boundary.
        /// </summary>
        public ILocation MaximumBounds { get; set; } = default!;

        /// <summary>
        /// Gets the initial direction the NPC should face upon spawning.
        /// </summary>
        public int? SpawnDirection { get; init; }
    }
}