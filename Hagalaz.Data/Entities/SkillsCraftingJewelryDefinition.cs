namespace Hagalaz.Data.Entities
{
    public partial class SkillsCraftingJewelryDefinition
    {
        public ushort ChildId { get; set; }
        public string Type { get; set; } = null!;
        public ushort ResourceId { get; set; }
        public ushort ProductId { get; set; }
        public byte RequiredLevel { get; set; }
        public decimal Experience { get; set; }
    }
}
