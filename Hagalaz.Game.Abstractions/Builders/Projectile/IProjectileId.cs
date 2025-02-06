using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Projectile
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IProjectileId
    {
        /// <summary>
        /// Sets the graphic id of the projectile.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IProjectileFrom WithGraphicId(int id);
    }
}