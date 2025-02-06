using System.Collections.Generic;

namespace Hagalaz.Data.Entities
{
    public partial class SkillsFishingSpotDefinition
    {
        public SkillsFishingSpotDefinition()
        {
            SkillsFishingFishDefinitions = new HashSet<SkillsFishingFishDefinition>();
            SkillsFishingSpotNpcDefinitions = new HashSet<SkillsFishingSpotNpcDefinition>();
        }

        public uint Id { get; set; }
        public string ClickType { get; set; } = null!;
        public decimal ExhaustChance { get; set; }
        public decimal BaseCatchChance { get; set; }
        public decimal RespawnTime { get; set; }
        public byte MinimumLevel { get; set; }
        public ushort ToolId { get; set; }
        public ushort? BaitId { get; set; }

        public virtual SkillsFishingToolDefinition Tool { get; set; } = null!;
        public virtual ICollection<SkillsFishingFishDefinition> SkillsFishingFishDefinitions { get; set; }
        public virtual ICollection<SkillsFishingSpotNpcDefinition> SkillsFishingSpotNpcDefinitions { get; set; }
    }
}
