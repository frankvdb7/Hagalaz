namespace Hagalaz.Data.Entities
{
    public partial class CharactersContact
    {
        public uint MasterId { get; set; }
        public uint ContactId { get; set; }
        public byte Type { get; set; }
        public sbyte FcRank { get; set; }

        public virtual Character Contact { get; set; } = null!;
        public virtual Character Master { get; set; } = null!;
    }
}
