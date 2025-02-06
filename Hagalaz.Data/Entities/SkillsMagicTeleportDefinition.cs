namespace Hagalaz.Data.Entities
{
    public partial class SkillsMagicTeleportDefinition
    {
        public short ButtonId { get; set; }
        public string SpellBook { get; set; } = null!;
        public ushort CoordX { get; set; }
        public ushort CoordY { get; set; }
        public byte CoordZ { get; set; }
        public ushort Distance { get; set; }
        public byte RequiredLevel { get; set; }
        public decimal Experience { get; set; }
        public string RequiredRunes { get; set; } = null!;
        public string RequiredRunesCount { get; set; } = null!;
    }
}
