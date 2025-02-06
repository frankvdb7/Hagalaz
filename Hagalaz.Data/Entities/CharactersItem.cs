namespace Hagalaz.Data.Entities
{
    public partial class CharactersItem
    {
        public uint Id { get; set; }
        public uint MasterId { get; set; }
        public ushort ItemId { get; set; }
        public int Count { get; set; }
        public ushort SlotId { get; set; }
        public sbyte ContainerType { get; set; }
        public string? ExtraData { get; set; }

        public virtual Character Master { get; set; } = null!;
    }
}
