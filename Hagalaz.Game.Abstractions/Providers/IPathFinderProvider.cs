using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;

namespace Hagalaz.Game.Abstractions.Providers
{
    public interface IPathFinderProvider
    {
        ISmartPathFinder Smart { get; }
        ISimplePathFinder Simple { get; }
        IProjectilePathFinder Projectile { get; }
    }
}
