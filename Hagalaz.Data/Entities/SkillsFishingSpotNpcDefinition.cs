namespace Hagalaz.Data.Entities
{
    public partial class SkillsFishingSpotNpcDefinition
    {
        public uint SpotId { get; set; }
        public ushort NpcId { get; set; }

        public virtual SkillsFishingSpotDefinition Spot { get; set; } = null!;
    }
}
