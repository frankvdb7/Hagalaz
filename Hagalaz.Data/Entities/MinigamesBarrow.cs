using System;

namespace Hagalaz.Data.Entities
{
    [Obsolete("Use Profile instead")]
    public partial class MinigamesBarrow
    {
        public uint MasterId { get; set; }
        public byte BrotherKilled0 { get; set; }
        public byte BrotherKilled1 { get; set; }
        public byte BrotherKilled2 { get; set; }
        public byte BrotherKilled3 { get; set; }
        public byte BrotherKilled4 { get; set; }
        public byte BrotherKilled5 { get; set; }
        public byte BrotherKilled6 { get; set; }
        public int KillCount { get; set; }
        public byte CryptStartIndex { get; set; }
        public byte TunnelIndex { get; set; }
        public byte LootedChest { get; set; }

        public virtual Character Master { get; set; } = null!;
    }
}
