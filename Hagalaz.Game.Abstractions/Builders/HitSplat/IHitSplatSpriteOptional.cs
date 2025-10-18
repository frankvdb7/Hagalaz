using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Combat;

namespace Hagalaz.Game.Abstractions.Builders.HitSplat
{
    /// <summary>
    /// Represents the step in the nested builder for a hitsplat sprite where optional parameters
    /// like damage, type, and critical status can be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IHitSplatBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IHitSplatSpriteOptional
    {
        /// <summary>
        /// Sets the numerical damage value to be displayed on the hitsplat sprite.
        /// </summary>
        /// <param name="damage">The amount of damage.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IHitSplatSpriteOptional WithDamage(int damage);

        /// <summary>
        /// Sets the type of the hitsplat, which determines its color and appearance (e.g., regular hit, poison, heal).
        /// </summary>
        /// <param name="type">The <see cref="HitSplatType"/> of the sprite.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IHitSplatSpriteOptional WithSplatType(HitSplatType type);

        /// <summary>
        /// Sets the type of damage, which determines the icon displayed on the hitsplat (e.g., melee, magic, ranged).
        /// </summary>
        /// <param name="damageType">The <see cref="DamageType"/> of the sprite.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IHitSplatSpriteOptional WithDamageType(DamageType damageType);

        /// <summary>
        /// Sets the maximum possible damage for this hit, used for visual scaling or effects.
        /// </summary>
        /// <param name="maxDamage">The maximum damage value.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IHitSplatSpriteOptional WithMaxDamage(int maxDamage);

        /// <summary>
        /// Marks the hitsplat as a critical hit, which may alter its appearance.
        /// </summary>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IHitSplatSpriteOptional AsCriticalDamage();
    }
}