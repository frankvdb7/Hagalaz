using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.Projectile
{
    /// <summary>
    /// Represents the final step in the fluent builder pattern for creating a projectile.
    /// This interface provides methods to either construct the <see cref="IProjectile"/> object
    /// or to construct and immediately send it to the client.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IProjectileBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IProjectileBuild
    {
        /// <summary>
        /// Builds the configured <see cref="IProjectile"/> instance without sending it.
        /// This is useful if the projectile object needs to be manipulated further before being sent.
        /// </summary>
        /// <returns>A new <see cref="IProjectile"/> object configured with the specified properties.</returns>
        IProjectile Build();

        /// <summary>
        /// Builds the configured <see cref="IProjectile"/> instance and immediately sends it to the relevant clients.
        /// This is a fire-and-forget method for creating and dispatching the projectile in one step.
        /// </summary>
        void Send();
    }
}