namespace Hagalaz.Data.Entities
{
    public partial class SkillsCraftingPotteryDefinition
    {
        public ushort FormedProductId { get; set; }
        public ushort BakedProductId { get; set; }
        public byte RequiredLevel { get; set; }
        public decimal FormExperience { get; set; }
        public decimal BakeExperience { get; set; }
    }
}
