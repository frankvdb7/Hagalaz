using System.Collections.Generic;

namespace Hagalaz.Data.Entities
{
    public partial class SkillsMiningOreDefinition
    {
        public SkillsMiningOreDefinition()
        {
            SkillsMiningRockDefinitions = new HashSet<SkillsMiningRockDefinition>();
        }

        public ushort ItemId { get; set; }
        public byte RequiredLevel { get; set; }
        public decimal Experience { get; set; }
        public decimal RespawnTime { get; set; }
        public decimal ExhaustChance { get; set; }
        public decimal BaseHarvestChance { get; set; }

        public virtual ICollection<SkillsMiningRockDefinition> SkillsMiningRockDefinitions { get; set; }
    }
}
