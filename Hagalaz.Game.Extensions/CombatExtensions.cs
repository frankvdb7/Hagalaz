using Hagalaz.Game.Abstractions.Model.Combat;

namespace Hagalaz.Game.Extensions
{
    /// <summary>
    /// Provides extension methods for combat-related enums and interfaces.
    /// </summary>
    public static class CombatExtensions
    {
        /// <summary>
        /// Converts a <see cref="DamageType"/> to its corresponding <see cref="HitSplatType"/>.
        /// This is used to determine the visual appearance of a hitsplat based on the type of damage dealt.
        /// </summary>
        /// <param name="damageType">The damage type to convert.</param>
        /// <returns>The corresponding hitsplat type.</returns>
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