namespace Hagalaz.Data.Entities
{
    public partial class SkillsCookingFoodDefinition
    {
        public ushort ItemId { get; set; }
        public byte HealAmount { get; set; }
        public short LeftItemId { get; set; }
        public uint EatingTime { get; set; }
    }
}
