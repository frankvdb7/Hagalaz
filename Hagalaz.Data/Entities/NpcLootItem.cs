namespace Hagalaz.Data.Entities
{
    public partial class NpcLootItem
    {
        public uint Id { get; set; }
        public ushort NpcLootId { get; set; }
        public ushort ItemId { get; set; }
        public uint MinimumCount { get; set; }
        public uint MaximumCount { get; set; }
        public decimal Probability { get; set; }
        public byte Always { get; set; }

        public virtual NpcLoot NpcLoot { get; set; } = null!;
    }
}
