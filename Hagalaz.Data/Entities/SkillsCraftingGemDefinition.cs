namespace Hagalaz.Data.Entities
{
    public partial class SkillsCraftingGemDefinition
    {
        public ushort ResourceId { get; set; }
        public ushort ProductId { get; set; }
        public short AnimationId { get; set; }
        public byte RequiredLevel { get; set; }
        public decimal Experience { get; set; }
    }
}
