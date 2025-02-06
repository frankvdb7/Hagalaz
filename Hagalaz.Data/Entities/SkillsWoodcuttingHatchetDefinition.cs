namespace Hagalaz.Data.Entities
{
    public partial class SkillsWoodcuttingHatchetDefinition
    {
        public string Type { get; set; } = null!;
        public ushort ItemId { get; set; }
        public ushort ChopAnimationId { get; set; }
        public ushort CanoueAnimationId { get; set; }
        public byte RequiredLevel { get; set; }
        public decimal BaseHarvestChance { get; set; }
    }
}
