namespace Hagalaz.Data.Entities
{
    public partial class SkillsPrayerDefinition
    {
        public ushort ItemId { get; set; }
        public decimal Experience { get; set; }
        public string Type { get; set; } = null!;
    }
}
