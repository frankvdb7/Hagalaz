using System;

namespace Hagalaz.Data.Entities
{
    [Obsolete("Use Profile instead")]
    public partial class MinigamesGodwar
    {
        public uint MasterId { get; set; }
        public short ArmadylKillCount { get; set; }
        public short BandosKillCount { get; set; }
        public short SaradominKillCount { get; set; }
        public short ZamorakKillCount { get; set; }

        public virtual Character Master { get; set; } = null!;
    }
}
