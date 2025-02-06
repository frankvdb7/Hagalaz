namespace Hagalaz.Data.Entities
{
    public partial class NpcSpawn
    {
        public uint SpawnId { get; set; }
        public ushort NpcId { get; set; }
        public short CoordX { get; set; }
        public short CoordY { get; set; }
        public byte CoordZ { get; set; }
        public short MinCoordX { get; set; }
        public short MinCoordY { get; set; }
        public byte MinCoordZ { get; set; }
        public short MaxCoordX { get; set; }
        public short MaxCoordY { get; set; }
        public byte MaxCoordZ { get; set; }
        public sbyte? SpawnDirection { get; set; }
    }
}
