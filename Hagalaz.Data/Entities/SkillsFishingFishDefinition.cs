namespace Hagalaz.Data.Entities
{
    public partial class SkillsFishingFishDefinition
    {
        public ushort ItemId { get; set; }
        public byte RequiredLevel { get; set; }
        public decimal Experience { get; set; }
        public uint SpotId { get; set; }
        public decimal Probability { get; set; }

        public virtual SkillsFishingSpotDefinition Spot { get; set; } = null!;
    }
}
