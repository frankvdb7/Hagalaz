namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// Contains information about a familiar.
    /// </summary>
    public record SummoningDto
    {
        /// <summary>
        /// The NPCID
        /// </summary>
        public int NpcId { get; set; }

        /// <summary>
        /// The pouch id
        /// </summary>
        public int PouchId { get; set; }

        /// <summary>
        /// The scroll id
        /// </summary>
        public int ScrollId { get; set; }

        /// <summary>
        /// The config id
        /// </summary>
        public int ConfigId { get; set; }

        /// <summary>
        /// The summon spawn cost
        /// </summary>
        public int SummonSpawnCost { get; set; }

        /// <summary>
        /// The summon level
        /// </summary>
        public int SummonLevel { get; set; }

        /// <summary>
        /// The summon experience
        /// </summary>
        public double SummonExperience { get; set; }

        /// <summary>
        /// The create experience
        /// </summary>
        public double CreatePouchExperience { get; set; }

        /// <summary>
        /// The scroll experience (using and creating)
        /// </summary>
        public double ScrollExperience { get; set; }

        /// <summary>
        /// The ticks
        /// </summary>
        public int Ticks { get; set; }
    }
}