using Hagalaz.Game.Abstractions.Model.Combat;

namespace Hagalaz.Game.Extensions
{
    public static class CombatExtensions
    {
        public static HitSplatType ToHitSplatType(this DamageType damageType) =>
            damageType switch
            {
                DamageType.StandardMelee => HitSplatType.HitMeleeDamage,
                DamageType.StandardRange => HitSplatType.HitRangeDamage,
                DamageType.StandardMagic => HitSplatType.HitMagicDamage,
                DamageType.StandardSummoning => HitSplatType.HitSimpleDamage,
                DamageType.Reflected => HitSplatType.HitDeflectDamage,
                DamageType.Standard => HitSplatType.HitSimpleDamage,
                DamageType.DragonFire => HitSplatType.HitMagicDamage, // Assuming DragonFire maps to magic
                DamageType.FullMelee => HitSplatType.HitMeleeDamage,
                DamageType.FullRange => HitSplatType.HitRangeDamage,
                DamageType.FullMagic => HitSplatType.HitMagicDamage,
                DamageType.FullSummoning => HitSplatType.HitSimpleDamage,
                _ => HitSplatType.None // Default case for unmapped types
            };
    }
}