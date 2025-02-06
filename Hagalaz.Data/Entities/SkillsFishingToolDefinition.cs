using System.Collections.Generic;

namespace Hagalaz.Data.Entities
{
    public partial class SkillsFishingToolDefinition
    {
        public SkillsFishingToolDefinition()
        {
            SkillsFishingSpotDefinitions = new HashSet<SkillsFishingSpotDefinition>();
        }

        public ushort ItemId { get; set; }
        public ushort FishAnimationId { get; set; }
        public ushort? CastAnimationId { get; set; }

        public virtual ICollection<SkillsFishingSpotDefinition> SkillsFishingSpotDefinitions { get; set; }
    }
}
