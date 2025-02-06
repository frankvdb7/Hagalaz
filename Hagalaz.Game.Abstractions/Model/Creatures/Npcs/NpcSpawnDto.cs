namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
{
    /// <summary>
    /// Class which contains data about npc spawn.
    /// </summary>
    public record NpcSpawnDto
    {
        /// <summary>
        /// Id of the npc.
        /// </summary>
        public int NpcId { get; init; }

        /// <summary>
        /// Location of the npc.
        /// </summary>
        public ILocation Location { get; set; } = default!;

        /// <summary>
        /// The minimum bounds of the npc.
        /// </summary>
        public ILocation MinimumBounds { get; set; } = default!;

        /// <summary>
        /// The maximum bounds of the npc.
        /// </summary>
        public ILocation MaximumBounds { get; set; } = default!;

        /// <summary>
        /// The spawn direction of the npc.
        /// </summary>
        public int? SpawnDirection { get; init; }
    }
}