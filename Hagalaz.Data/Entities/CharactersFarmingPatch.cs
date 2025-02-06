namespace Hagalaz.Data.Entities
{
    public partial class CharactersFarmingPatch
    {
        public uint MasterId { get; set; }
        public uint PatchId { get; set; }
        public ushort SeedId { get; set; }
        public uint ConditionFlag { get; set; }
        public byte CurrentCycle { get; set; }
        public uint CurrentCycleTicks { get; set; }
        public uint ProductCount { get; set; }

        public virtual Character Master { get; set; } = null!;
        public virtual SkillsFarmingPatchDefinition Patch { get; set; } = null!;
        public virtual SkillsFarmingSeedDefinition Seed { get; set; } = null!;
    }
}
