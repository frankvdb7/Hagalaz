namespace Hagalaz.Data.Entities
{
    public partial class CharactersQuest
    {
        public uint MasterId { get; set; }
        public ushort QuestId { get; set; }
        public byte Status { get; set; }
        public ushort Stage { get; set; }

        public virtual Character Master { get; set; } = null!;
        public virtual Quest Quest { get; set; } = null!;
    }
}
