using System;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Providers
{
    public class PathFinderProvider : IPathFinderProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public PathFinderProvider(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public ISmartPathFinder Smart => _serviceProvider.GetRequiredService<ISmartPathFinder>();

        public ISimplePathFinder Simple => _serviceProvider.GetRequiredService<ISimplePathFinder>();

        public IProjectilePathFinder Projectile => _serviceProvider.GetRequiredService<IProjectilePathFinder>();
    }
}
