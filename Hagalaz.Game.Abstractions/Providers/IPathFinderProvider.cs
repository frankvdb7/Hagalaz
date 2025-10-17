using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// Defines a contract for a provider that offers access to different types of pathfinding algorithms.
    /// </summary>
    public interface IPathFinderProvider
    {
        /// <summary>
        /// Gets the smart pathfinder, typically used for complex NPC and character movement that considers obstacles.
        /// </summary>
        ISmartPathFinder Smart { get; }

        /// <summary>
        /// Gets the simple pathfinder, used for basic, direct-line movement calculations where obstacles are not a concern.
        /// </summary>
        ISimplePathFinder Simple { get; }

        /// <summary>
        /// Gets the projectile pathfinder, specialized for calculating the trajectory of projectiles, which may ignore certain obstacles.
        /// </summary>
        IProjectilePathFinder Projectile { get; }
    }
}
