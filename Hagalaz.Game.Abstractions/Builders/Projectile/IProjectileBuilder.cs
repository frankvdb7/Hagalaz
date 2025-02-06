namespace Hagalaz.Game.Abstractions.Builders.Projectile
{
    public interface IProjectileBuilder
    {
        /// <summary>
        /// Begins the build sequence to create a projectile
        /// </summary>
        /// <returns></returns>
        IProjectileId Create();
    }
}