namespace Hagalaz.Data.Entities
{
    public partial class CharactersFamiliar
    {
        public uint MasterId { get; set; }
        public ushort FamiliarId { get; set; }
        public ushort SpecialMovePoints { get; set; }
        public byte UsingSpecialMove { get; set; }
        public uint TicksRemaining { get; set; }

        public virtual NpcDefinition Familiar { get; set; } = null!;
        public virtual Character Master { get; set; } = null!;
    }
}
