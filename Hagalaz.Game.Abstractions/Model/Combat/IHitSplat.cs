namespace Hagalaz.Game.Abstractions.Model.Combat
{
    /// <summary>
    /// Defines the contract for a hitsplat, which is a visual indicator of damage or healing.
    /// A single hitsplat event can contain up to two individual splats.
    /// </summary>
    public interface IHitSplat
    {
        /// <summary>
        /// Gets the entity that is the source of the damage or effect.
        /// </summary>
        IRuneObject? Sender { get; }

        /// <summary>
        /// Gets the type of the first (primary) splat, which determines its color and icon.
        /// </summary>
        HitSplatType FirstSplatType { get; }

        /// <summary>
        /// Gets the type of the second (secondary) splat, if one exists.
        /// </summary>
        HitSplatType SecondSplatType { get; }

        /// <summary>
        /// Gets the delay in game ticks before the hitsplat is displayed.
        /// </summary>
        int Delay { get; }

        /// <summary>
        /// Gets a value indicating whether the first splat represents a critical hit.
        /// </summary>
        bool FirstSplatCritical { get; }

        /// <summary>
        /// Gets the numerical value displayed on the first splat (e.g., the amount of damage or healing).
        /// </summary>
        int FirstSplatDamage { get; }

        /// <summary>
        /// Gets a value indicating whether the second splat represents a critical hit.
        /// </summary>
        bool SecondSplatCritical { get; }

        /// <summary>
        /// Gets the numerical value displayed on the second splat, if one exists.
        /// </summary>
        int SecondSplatDamage { get; }
    }
}
