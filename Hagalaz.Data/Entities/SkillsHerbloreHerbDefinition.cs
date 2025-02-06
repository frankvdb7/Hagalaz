namespace Hagalaz.Data.Entities
{
    public partial class SkillsHerbloreHerbDefinition
    {
        public ushort GrimyItemId { get; set; }
        public ushort CleanItemId { get; set; }
        public byte RequiredLevel { get; set; }
        public decimal Experience { get; set; }
    }
}
