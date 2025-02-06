namespace Hagalaz.Data.Entities
{
    public partial class NpcStatistic
    {
        public ushort NpcId { get; set; }
        public ushort MaxLifepoints { get; set; }
        public ushort AttackLevel { get; set; }
        public ushort DefenceLevel { get; set; }
        public ushort StrengthLevel { get; set; }
        public ushort RangedLevel { get; set; }
        public ushort MagicLevel { get; set; }
    }
}
