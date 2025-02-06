namespace Hagalaz.Data.Entities
{
    public partial class CharactersMusic
    {
        public uint MasterId { get; set; }
        public string UnlockedMusic { get; set; } = null!;

        public virtual Character Master { get; set; } = null!;
    }
}
