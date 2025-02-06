namespace Hagalaz.Data.Entities
{
    public partial class GameobjectLootItem
    {
        public uint Id { get; set; }
        public ushort GameobjectLootId { get; set; }
        public ushort ItemId { get; set; }
        public uint MinimumCount { get; set; }
        public uint MaximumCount { get; set; }
        public decimal Probability { get; set; }
        public byte Always { get; set; }

        public virtual GameobjectLoot GameobjectLoot { get; set; } = null!;
    }
}
