using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Services
{
    /// <summary>
    /// Services the map regions. This helps keep the server free of some
    /// space, by idling and killing regions that have been inactive.
    /// </summary>
    public sealed class MapRegionBackgroundService : BackgroundService
    {
        private static readonly TimeSpan ProcessingInterval = TimeSpan.FromMinutes(5);
        private readonly IMapRegionService _regionService;
        private readonly ILogger<MapRegionBackgroundService> _logger;
        private readonly Channel<IMapRegion> _detachedRegions = Channel.CreateUnbounded<IMapRegion>();
        private DateTime _lastProcessedAt = DateTime.MinValue;

        public MapRegionBackgroundService(IMapRegionService regionService, ILogger<MapRegionBackgroundService> logger)
        {
            _regionService = regionService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var region in _detachedRegions.Reader.ReadAllAsync(stoppingToken))
                {
                    DestroyDetachedRegion(region);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
        }

        internal async Task ProcessRegionsIfDueAsync(IReadOnlyDictionary<int, ICharacter> characters)
        {
            var now = DateTime.UtcNow;
            if (now - _lastProcessedAt < ProcessingInterval)
            {
                return;
            }

            await ProcessRegionsOnceAsync(characters);
            _lastProcessedAt = now;
        }

        internal Task ProcessRegionsOnceAsync(IReadOnlyDictionary<int, ICharacter> characters)
        {
            var visibleRegions = new HashSet<IMapRegion>(ReferenceEqualityComparer.Instance);
            foreach (var character in characters.Values)
            {
                visibleRegions.UnionWith(character.Viewport.VisibleRegions);
            }

            foreach (var dimension in _regionService.FindAllDimensions())
            {
                foreach (var region in _regionService.FindRegionsByDimension(dimension.Id)
                             .Where(region => region.State == MapRegionState.Ready))
                {
                    if (visibleRegions.Contains(region))
                    {
                        continue;
                    }

                    if (_regionService.TrySuspendMapRegion(region))
                    {
                        _logger.LogDebug("Region[{id}] was suspended.", region.Id);
                    }
                }

                foreach (var region in _regionService.FindIdleRegionsByDimension(dimension.Id).Where(region => region.CanDestroy()))
                {
                    if (visibleRegions.Contains(region))
                    {
                        continue;
                    }

                    if (!_regionService.TryRemoveIdleMapRegion(region.Id, dimension.Id, region))
                    {
                        continue;
                    }

                    EnqueueDetachedRegion(region);
                }

                if (_regionService.TryRemoveEmptyDimension(dimension))
                {
                    _logger.LogDebug("Dimension[{id}] was destroyed.", dimension.Id);
                }
            }

            return Task.CompletedTask;
        }

        private void DestroyDetachedRegion(IMapRegion region)
        {
            try
            {
                region.Destroy();
                _logger.LogDebug("Region[{id}] was destroyed.", region.Id);
            }
            catch (Exception ex)
            {
                // Destruction is terminal and is not replayed: resource teardown
                // can already have removed entities and disposed their scopes.
                _logger.LogError(ex, "Failed to destroy detached region[{id}].", region.Id);
            }
        }

        private void EnqueueDetachedRegion(IMapRegion region)
        {
            _detachedRegions.Writer.TryWrite(region);
        }
    }
}
