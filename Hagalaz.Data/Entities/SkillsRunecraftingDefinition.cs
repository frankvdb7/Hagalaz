namespace Hagalaz.Data.Entities
{
    public partial class SkillsRunecraftingDefinition
    {
        public uint AltarId { get; set; }
        public uint RuinId { get; set; }
        public uint PortalId { get; set; }
        public uint RiftId { get; set; }
        public ushort RuneId { get; set; }
        public ushort TalismanId { get; set; }
        public ushort TiaraId { get; set; }
        public byte RequiredLevel { get; set; }
        public decimal Experience { get; set; }
        public string LevelCountMultipliers { get; set; } = null!;
        public string AltarLocation { get; set; } = null!;
        public string RuinLocation { get; set; } = null!;
        public string RiftLocation { get; set; } = null!;
    }
}
