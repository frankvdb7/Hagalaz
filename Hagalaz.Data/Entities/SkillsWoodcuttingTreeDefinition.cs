namespace Hagalaz.Data.Entities
{
    public partial class SkillsWoodcuttingTreeDefinition
    {
        public uint TreeId { get; set; }
        public uint StumpId { get; set; }
        public ushort LogId { get; set; }

        public virtual SkillsWoodcuttingLogDefinition Log { get; set; } = null!;
        public virtual GameobjectDefinition Tree { get; set; } = null!;
    }
}
