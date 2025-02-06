namespace Hagalaz.Data.Entities
{
    public partial class SkillsMagicCombatDefinition
    {
        public ushort ButtonId { get; set; }
        public uint BaseDamage { get; set; }
        public byte RequiredLevel { get; set; }
        public decimal BaseExperience { get; set; }
        public string RequiredRunes { get; set; } = null!;
        public string RequiredRunesCounts { get; set; } = null!;
        public short CastAnimationId { get; set; }
        public short CastGraphicId { get; set; }
        public short EndGraphicId { get; set; }
        public short ProjectileId { get; set; }
        public uint AutocastConfig { get; set; }
    }
}
