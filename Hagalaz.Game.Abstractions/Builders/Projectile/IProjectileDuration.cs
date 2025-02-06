using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Projectile
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IProjectileDuration
    {
        /// <summary>
        /// Sets the projectiles duration.
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        public IProjectileOptional WithDuration(int duration);
    }
}