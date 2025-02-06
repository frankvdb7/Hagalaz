namespace Hagalaz.Data.Entities
{
    public partial class SkillsMiningPickaxeDefinition
    {
        public string Type { get; set; } = null!;
        public ushort ItemId { get; set; }
        public ushort AnimationId { get; set; }
        public byte RequiredLevel { get; set; }
        public decimal BaseHarvestChance { get; set; }
    }
}
