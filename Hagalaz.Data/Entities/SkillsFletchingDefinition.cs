namespace Hagalaz.Data.Entities
{
    public partial class SkillsFletchingDefinition
    {
        public ushort ResourceId { get; set; }
        public ushort ToolId { get; set; }
        public string ProductIds { get; set; } = null!;
        public string ProductCounts { get; set; } = null!;
        public string ProductExperiences { get; set; } = null!;
        public string RequiredLevels { get; set; } = null!;
        public ushort AnimationId { get; set; }
    }
}
