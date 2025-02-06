using System;

namespace Hagalaz.Data.Entities
{
    [Obsolete("Use Profile instead")]
    public partial class MinigamesTzhaarCave
    {
        public uint MasterId { get; set; }
        public uint CurrentWaveId { get; set; }

        public virtual Character Master { get; set; } = null!;
    }
}
