namespace Hagalaz.Data.Entities
{
    public partial class CharactersState
    {
        public uint MasterId { get; set; }
        public string StateId { get; set; } = null!;
        public int TicksLeft { get; set; }

        public virtual Character Master { get; set; } = null!;
    }
}
