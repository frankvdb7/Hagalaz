namespace Hagalaz.Data.Entities
{
    public partial class SkillsCraftingSpinDefinition
    {
        public ushort ResourceId { get; set; }
        public ushort ProductId { get; set; }
        public byte RequiredLevel { get; set; }
        public decimal Experience { get; set; }
    }
}
