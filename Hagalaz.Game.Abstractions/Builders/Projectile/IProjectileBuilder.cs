namespace Hagalaz.Game.Abstractions.Builders.Projectile
{
    /// <summary>
    /// Defines the contract for a projectile builder, which serves as the entry point
    /// for constructing an <see cref="Model.IProjectile"/> object using a fluent interface.
    /// </summary>
    public interface IProjectileBuilder
    {
        /// <summary>
        /// Begins the process of building a new projectile.
        /// </summary>
        /// <returns>The next step in the fluent builder chain, which requires specifying the projectile's graphic ID.</returns>
        IProjectileId Create();
    }
}