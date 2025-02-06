namespace Hagalaz.Data.Entities
{
    public partial class ItemLootItem
    {
        public uint Id { get; set; }
        public ushort ItemLootId { get; set; }
        public ushort ItemId { get; set; }
        public uint MinimumCount { get; set; }
        public uint MaximumCount { get; set; }
        public decimal Probability { get; set; }
        public byte Always { get; set; }

        public virtual ItemLoot ItemLoot { get; set; } = null!;
    }
}
