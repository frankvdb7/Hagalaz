namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition of a Summoning familiar.
    /// </summary>
    public record SummoningDto
    {
        /// <summary>
        /// Gets or sets the NPC ID of the familiar.
        /// </summary>
        public int NpcId { get; set; }

        /// <summary>
        /// Gets or sets the item ID of the summoning pouch.
        /// </summary>
        public int PouchId { get; set; }

        /// <summary>
        /// Gets or sets the item ID of the special move scroll.
        /// </summary>
        public int ScrollId { get; set; }

        /// <summary>
        /// Gets or sets the client configuration ID for this familiar.
        /// </summary>
        public int ConfigId { get; set; }

        /// <summary>
        /// Gets or sets the number of summoning points required to summon this familiar.
        /// </summary>
        public int SummonSpawnCost { get; set; }

        /// <summary>
        /// Gets or sets the required Summoning level to summon this familiar.
        /// </summary>
        public int SummonLevel { get; set; }

        /// <summary>
        /// Gets or sets the Summoning experience gained for summoning this familiar.
        /// </summary>
        public double SummonExperience { get; set; }

        /// <summary>
        /// Gets or sets the Summoning experience gained for creating the pouch for this familiar.
        /// </summary>
        public double CreatePouchExperience { get; set; }

        /// <summary>
        /// Gets or sets the Summoning experience gained for creating or using the special move scroll.
        /// </summary>
        public double ScrollExperience { get; set; }

        /// <summary>
        /// Gets or sets the duration in game ticks that the familiar will last.
        /// </summary>
        public int Ticks { get; set; }
    }
}