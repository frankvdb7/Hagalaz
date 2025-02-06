namespace Hagalaz.Data.Entities
{
    public partial class SkillsSummoningDefinition
    {
        public ushort NpcId { get; set; }
        public ushort PouchId { get; set; }
        public ushort ScrollId { get; set; }
        public ushort ConfigId { get; set; }
        public byte SummonSpawnCost { get; set; }
        public byte SummonLevel { get; set; }
        public decimal SummonExperience { get; set; }
        public decimal CreatePouchExperience { get; set; }
        public decimal ScrollExperience { get; set; }
        public uint Ticks { get; set; }
    }
}
