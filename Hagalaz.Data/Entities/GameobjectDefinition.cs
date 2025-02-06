namespace Hagalaz.Data.Entities
{
    public partial class GameobjectDefinition
    {
        public uint GameobjectId { get; set; }
        public string Name { get; set; } = null!;
        public string Examine { get; set; } = null!;
        public ushort? GameobjectLootId { get; set; }

        public virtual GameobjectLoot? GameobjectLoot { get; set; }
        public virtual SkillsMiningRockDefinition SkillsMiningRockDefinition { get; set; } = null!;
        public virtual SkillsWoodcuttingTreeDefinition SkillsWoodcuttingTreeDefinition { get; set; } = null!;
    }
}
