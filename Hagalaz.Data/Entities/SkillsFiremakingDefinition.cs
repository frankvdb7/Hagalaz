namespace Hagalaz.Data.Entities
{
    public partial class SkillsFiremakingDefinition
    {
        public ushort ItemId { get; set; }
        public byte RequiredLevel { get; set; }
        public decimal Experience { get; set; }
        public uint FireObjectId { get; set; }
        public uint Ticks { get; set; }
    }
}
