namespace Hagalaz.Data.Entities
{
    public partial class EquipmentBonuse
    {
        public ushort ItemId { get; set; }
        public int AttackStab { get; set; }
        public int AttackSlash { get; set; }
        public int AttackCrush { get; set; }
        public int AttackMagic { get; set; }
        public int AttackRanged { get; set; }
        public int DefenceStab { get; set; }
        public int DefenceSlash { get; set; }
        public int DefenceCrush { get; set; }
        public int DefenceMagic { get; set; }
        public int DefenceRanged { get; set; }
        public int DefenceSummoning { get; set; }
        public int AbsorbMelee { get; set; }
        public int AbsorbMagic { get; set; }
        public int AbsorbRange { get; set; }
        public int Strength { get; set; }
        public int RangedStrength { get; set; }
        public int Prayer { get; set; }
        public int MagicDamage { get; set; }
    }
}
