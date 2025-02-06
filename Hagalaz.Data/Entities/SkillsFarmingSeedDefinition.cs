using System.Collections.Generic;

namespace Hagalaz.Data.Entities
{
    public partial class SkillsFarmingSeedDefinition
    {
        public SkillsFarmingSeedDefinition()
        {
            CharactersFarmingPatches = new HashSet<CharactersFarmingPatch>();
        }

        public ushort ItemId { get; set; }
        public ushort ProductId { get; set; }
        public uint MinimumProductCount { get; set; }
        public uint MaximumProductCount { get; set; }
        public byte RequiredLevel { get; set; }
        public decimal PlantingExperience { get; set; }
        public decimal HarvestExperience { get; set; }
        public sbyte VarpbitIndex { get; set; }
        public byte MaxCycles { get; set; }
        public uint CycleTicks { get; set; }
        public string Type { get; set; } = null!;

        public virtual ICollection<CharactersFarmingPatch> CharactersFarmingPatches { get; set; }
    }
}
