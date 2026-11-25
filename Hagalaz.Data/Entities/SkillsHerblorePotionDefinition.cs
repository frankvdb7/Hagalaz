namespace Hagalaz.Data.Entities
{
    public partial class SkillsHerblorePotionDefinition
    {
        public ushort PotionId { get; set; }
        public byte Kind { get; set; }
        public ushort UnfinishedPotionId { get; set; }
        public byte RequiredLevel { get; set; }
        public decimal Experience { get; set; }
        public string PrimaryItemIds { get; set; } = null!;
        public string SecondaryItemIds { get; set; } = null!;
        public byte SecondaryItemCount { get; set; }
    }
}
