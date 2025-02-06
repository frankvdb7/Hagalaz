namespace Hagalaz.Data.Entities
{
    public partial class SkillsMiningRockDefinition
    {
        public uint RockId { get; set; }
        public uint ExhaustRockId { get; set; }
        public ushort OreId { get; set; }

        public virtual SkillsMiningOreDefinition Ore { get; set; } = null!;
        public virtual GameobjectDefinition Rock { get; set; } = null!;
    }
}
