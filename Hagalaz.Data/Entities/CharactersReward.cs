namespace Hagalaz.Data.Entities
{
    public partial class CharactersReward
    {
        public uint Id { get; set; }
        public uint MasterId { get; set; }
        public ushort ItemId { get; set; }
        public int Count { get; set; }
        public string ExtraData { get; set; } = null!;
        public byte Loaded { get; set; }

        public virtual Character Master { get; set; } = null!;
    }
}
