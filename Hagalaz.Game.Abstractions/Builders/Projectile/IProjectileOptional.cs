using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Projectile
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IProjectileOptional : IProjectileBuild
    {
        /// <summary>
        /// Whether to adjust the flying height automatically.
        /// </summary>
        /// <returns></returns>
        IProjectileOptional AdjustFromFlyingHeight();
        /// <summary>
        /// Whether to adjust the flying height automatically.
        /// </summary>
        /// <returns></returns>
        IProjectileOptional AdjustToFlyingHeight();
        /// <summary>
        /// Sets the delay of the projectile.
        /// </summary>
        /// <param name="delay"></param>
        /// <returns></returns>
        IProjectileOptional WithDelay(int delay);
        /// <summary>
        /// Sets the slope of the projectile.
        /// </summary>
        /// <param name="slope"></param>
        /// <returns></returns>
        IProjectileOptional WithSlope(int slope);
        /// <summary>
        /// Sets the angle of the projectile.
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        IProjectileOptional WithAngle(int angle);
        /// <summary>
        /// Sets the height the projectile will start with.
        /// </summary>
        /// <param name="height"></param>
        /// <returns></returns>
        IProjectileOptional WithFromHeight(int height);
        /// <summary>
        /// Sets the height the projectile will end with.
        /// </summary>
        /// <param name="height"></param>
        /// <returns></returns>
        IProjectileOptional WithToHeight(int height);
    }
}