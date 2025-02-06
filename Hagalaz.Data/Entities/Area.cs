namespace Hagalaz.Data.Entities
{
    public partial class Area
    {
        public ushort Id { get; set; }
        public string Name { get; set; } = null!;
        public short MinimumX { get; set; }
        public short MaximumX { get; set; }
        public short MinimumY { get; set; }
        public short MaximumY { get; set; }
        public byte MinimumZ { get; set; }
        public byte MaximumZ { get; set; }
        public ushort MinimumDimension { get; set; }
        public ushort MaximumDimension { get; set; }
        public byte PvpAllowed { get; set; }
        public byte MulticombatAllowed { get; set; }
        public byte CannonAllowed { get; set; }
        public byte FamiliarAllowed { get; set; }
    }
}
