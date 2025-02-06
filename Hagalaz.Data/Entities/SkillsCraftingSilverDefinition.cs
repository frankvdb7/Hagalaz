namespace Hagalaz.Data.Entities
{
    public partial class SkillsCraftingSilverDefinition
    {
        public ushort ChildId { get; set; }
        public ushort MouldId { get; set; }
        public ushort ProductId { get; set; }
        public byte RequiredLevel { get; set; }
        public decimal Experience { get; set; }
    }
}
