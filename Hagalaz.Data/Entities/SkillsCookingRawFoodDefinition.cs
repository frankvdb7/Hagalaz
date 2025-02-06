namespace Hagalaz.Data.Entities
{
    public partial class SkillsCookingRawFoodDefinition
    {
        public ushort ItemId { get; set; }
        public ushort CookedItemId { get; set; }
        public ushort BurntItemId { get; set; }
        public byte RequiredLevel { get; set; }
        public byte StopBurningLevel { get; set; }
        public decimal Experience { get; set; }
    }
}
