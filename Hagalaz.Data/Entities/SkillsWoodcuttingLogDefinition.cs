using System.Collections.Generic;

namespace Hagalaz.Data.Entities
{
    public partial class SkillsWoodcuttingLogDefinition
    {
        public SkillsWoodcuttingLogDefinition()
        {
            SkillsWoodcuttingTreeDefinitions = new HashSet<SkillsWoodcuttingTreeDefinition>();
        }

        public ushort ItemId { get; set; }
        public byte RequiredLevel { get; set; }
        public decimal Experience { get; set; }
        /// <summary>
        /// Respawn time in minutes
        /// </summary>
        public decimal RespawnTime { get; set; }
        public decimal FallChance { get; set; }
        public decimal BaseHarvestChance { get; set; }

        public virtual ICollection<SkillsWoodcuttingTreeDefinition> SkillsWoodcuttingTreeDefinitions { get; set; }
    }
}
