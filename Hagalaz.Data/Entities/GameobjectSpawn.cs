namespace Hagalaz.Data.Entities
{
    public partial class GameobjectSpawn
    {
        public int SpawnId { get; set; }
        public uint GameobjectId { get; set; }
        public short CoordX { get; set; }
        public short CoordY { get; set; }
        public ushort CoordZ { get; set; }
        public short Face { get; set; }
        public short Type { get; set; }
    }
}
