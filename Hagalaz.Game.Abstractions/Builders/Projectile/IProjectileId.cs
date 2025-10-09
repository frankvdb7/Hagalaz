using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Projectile
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a projectile where the graphic ID must be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IProjectileBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IProjectileId
    {
        /// <summary>
        /// Sets the graphic identifier for the projectile, determining its visual appearance.
        /// </summary>
        /// <param name="id">The unique identifier for the projectile's graphic.</param>
        /// <returns>The next step in the fluent builder chain, which requires specifying the projectile's origin.</returns>
        IProjectileFrom WithGraphicId(int id);
    }
}