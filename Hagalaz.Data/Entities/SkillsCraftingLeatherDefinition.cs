namespace Hagalaz.Data.Entities
{
    public partial class SkillsCraftingLeatherDefinition
    {
        public ushort ProductId { get; set; }
        public ushort ResourceId { get; set; }
        public uint RequiredResourceCount { get; set; }
        public byte RequiredLevel { get; set; }
        public decimal Experience { get; set; }
    }
}
