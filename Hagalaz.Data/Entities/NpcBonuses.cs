namespace Hagalaz.Data.Entities
{
    public partial class NpcBonuses
    {
        public ushort NpcId { get; set; }
        public short AttackStab { get; set; }
        public short AttackSlash { get; set; }
        public short AttackCrush { get; set; }
        public short AttackMagic { get; set; }
        public short AttackRanged { get; set; }
        public short DefenceStab { get; set; }
        public short DefenceSlash { get; set; }
        public short DefenceCrush { get; set; }
        public short DefenceMagic { get; set; }
        public short DefenceRanged { get; set; }
        public short DefenceSummoning { get; set; }
        public short AbsorbMelee { get; set; }
        public short AbsorbMagic { get; set; }
        public short AbsorbRange { get; set; }
        public short Strength { get; set; }
        public short RangedStrength { get; set; }
        public short Prayer { get; set; }
        public short Magic { get; set; }
    }
}
