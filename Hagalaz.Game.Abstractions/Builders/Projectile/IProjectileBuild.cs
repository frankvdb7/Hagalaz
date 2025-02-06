using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.Projectile
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IProjectileBuild
    {
        /// <summary>
        /// Builds the final projectile entity.
        /// </summary>
        /// <returns></returns>
        IProjectile Build();

        /// <summary>
        /// Sends the projectile to the client.
        /// </summary>
        void Send();
    }
}