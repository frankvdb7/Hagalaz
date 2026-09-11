using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Services
{
    /// <summary>
    /// Services the map regions. This helps keep the server free of some
    /// space, by idling and killing regions that have been inactive.
    /// </summary>
    public class MapRegionBackgroundService
    {
        private static readonly TimeSpan ProcessingInterval = TimeSpan.FromMinutes(5);
        private readonly IMapRegionService _regionService;
        private readonly ILogger<MapRegionBackgroundService> _logger;
        private DateTime _lastProcessedAt = DateTime.MinValue;

        public MapRegionBackgroundService(IMapRegionService regionService, ILogger<MapRegionBackgroundService> logger)
        {
            _regionService = regionService;
            _logger = logger;
        }

        internal async Task ProcessRegionsIfDueAsync()
        {
            var now = DateTime.UtcNow;
            if (now - _lastProcessedAt < ProcessingInterval)
            {
                return;
            }

            await ProcessRegionsOnceAsync();
            _lastProcessedAt = now;
        }

        internal async Task ProcessRegionsOnceAsync()
        {
            foreach (var dimension in _regionService.FindAllDimensions())
            {
                foreach (var region in _regionService.FindRegionsByDimension(dimension.Id)
                             .Where(region => region.State == MapRegionState.Ready && region.CanSuspend()))
                {
                    if (_regionService.TrySuspendMapRegion(region))
                    {
                        _logger.LogDebug("Region[{id}] was suspended.", region.Id);
                    }
                }

                foreach (var region in _regionService.FindIdleRegionsByDimension(dimension.Id).Where(region => region.CanDestroy()))
                {
                    if (!_regionService.TryRemoveIdleMapRegion(region.Id, dimension.Id, region))
                    {
                        continue;
                    }

                    try
                    {
                        await region.DestroyAsync();
                        _logger.LogDebug("Region[{id}] was destroyed.", region.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to destroy region[{id}] in dimension[{dimension}]; its ownership was already released.", region.Id, dimension.Id);
                    }
                }

                if (_regionService.TryRemoveEmptyDimension(dimension))
                {
                    _logger.LogDebug("Dimension[{id}] was destroyed.", dimension.Id);
                }
            }
        }
    }
}
