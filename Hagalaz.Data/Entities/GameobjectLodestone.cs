namespace Hagalaz.Data.Entities
{
    public partial class GameobjectLodestone
    {
        public uint GameobjectId { get; set; }
        public ushort ButtonId { get; set; }
        public uint StateId { get; set; }
        public ushort CoordX { get; set; }
        public ushort CoordY { get; set; }
        public byte CoordZ { get; set; }
    }
}
