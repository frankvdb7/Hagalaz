namespace Hagalaz.Data.Entities
{
    public partial class CharactersItemsLook
    {
        public uint MasterId { get; set; }
        public ushort ItemId { get; set; }
        public int MaleWornModel1 { get; set; }
        public int MaleWornModel2 { get; set; }
        public int FemaleWornModel1 { get; set; }
        public int FemaleWornModel2 { get; set; }
        public int MaleWornModel3 { get; set; }
        public int FemaleWornModel3 { get; set; }
        public string? ModelColours { get; set; }
        public string? TextureColours { get; set; }

        public virtual Character Master { get; set; } = null!;
    }
}
