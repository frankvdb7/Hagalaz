using System.Collections.Generic;

namespace Hagalaz.Data.Entities
{
    public partial class SkillsSlayerMasterDefinition
    {
        public SkillsSlayerMasterDefinition()
        {
            SkillsSlayerTaskDefinitions = new HashSet<SkillsSlayerTaskDefinition>();
        }

        public ushort NpcId { get; set; }
        public string Name { get; set; } = null!;
        public uint BaseSlayerRewardPoints { get; set; }

        public virtual ICollection<SkillsSlayerTaskDefinition> SkillsSlayerTaskDefinitions { get; set; }
    }
}
