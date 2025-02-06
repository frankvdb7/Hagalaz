namespace Hagalaz.Data.Entities
{
    public partial class ItemSpawn
    {
        public ushort ItemId { get; set; }
        public int Count { get; set; }
        public short CoordX { get; set; }
        public short CoordY { get; set; }
        public byte CoordZ { get; set; }
        public uint RespawnTicks { get; set; }
    }
}
