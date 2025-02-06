namespace Hagalaz.Data.Entities
{
    public partial class CharactersLook
    {
        public uint MasterId { get; set; }
        public short Gender { get; set; }
        public short HairColor { get; set; }
        public short TorsoColor { get; set; }
        public short LegColor { get; set; }
        public short FeetColor { get; set; }
        public short SkinColor { get; set; }
        public int HairLook { get; set; }
        public int BeardLook { get; set; }
        public int TorsoLook { get; set; }
        public int ArmsLook { get; set; }
        public int WristLook { get; set; }
        public int LegsLook { get; set; }
        public int FeetLook { get; set; }
        public byte DisplayTitle { get; set; }

        public virtual Character Master { get; set; } = null!;
    }
}
