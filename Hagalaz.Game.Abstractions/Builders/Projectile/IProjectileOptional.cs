using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Projectile
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a projectile where optional
    /// parameters like flight path, delay, and height can be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IProjectileBuilder"/>.
    /// It also inherits from <see cref="IProjectileBuild"/>, allowing the build process to be finalized at this stage.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IProjectileOptional : IProjectileBuild
    {
        /// <summary>
        /// Automatically adjusts the projectile's starting height based on the source's flying height, if applicable.
        /// </summary>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IProjectileOptional AdjustFromFlyingHeight();

        /// <summary>
        /// Automatically adjusts the projectile's ending height based on the target's flying height, if applicable.
        /// </summary>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IProjectileOptional AdjustToFlyingHeight();

        /// <summary>
        /// Sets the delay before the projectile is launched.
        /// </summary>
        /// <param name="delay">The delay in game ticks.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IProjectileOptional WithDelay(int delay);

        /// <summary>
        /// Sets the slope or arc of the projectile's flight path.
        /// </summary>
        /// <param name="slope">The slope value, affecting the curvature of the projectile's trajectory.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IProjectileOptional WithSlope(int slope);

        /// <summary>
        /// Sets the initial angle of the projectile.
        /// </summary>
        /// <param name="angle">The launch angle of the projectile.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IProjectileOptional WithAngle(int angle);

        /// <summary>
        /// Sets a specific starting height for the projectile, overriding any automatic adjustments.
        /// </summary>
        /// <param name="height">The starting height of the projectile.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IProjectileOptional WithFromHeight(int height);

        /// <summary>
        /// Sets a specific ending height for the projectile, overriding any automatic adjustments.
        /// </summary>
        /// <param name="height">The ending height of the projectile.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IProjectileOptional WithToHeight(int height);
    }
}