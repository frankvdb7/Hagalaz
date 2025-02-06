using System.Collections.Generic;

namespace Hagalaz.Data.Entities
{
    public partial class SkillsFarmingPatchDefinition
    {
        public SkillsFarmingPatchDefinition()
        {
            CharactersFarmingPatches = new HashSet<CharactersFarmingPatch>();
        }

        public uint ObjectId { get; set; }
        public string Type { get; set; } = null!;

        public virtual ICollection<CharactersFarmingPatch> CharactersFarmingPatches { get; set; }
    }
}
