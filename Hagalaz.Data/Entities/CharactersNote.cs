namespace Hagalaz.Data.Entities
{
    public partial class CharactersNote
    {
        public uint MasterId { get; set; }
        public byte NoteId { get; set; }
        public byte Colour { get; set; }
        public string Text { get; set; } = null!;

        public virtual Character Master { get; set; } = null!;
    }
}
