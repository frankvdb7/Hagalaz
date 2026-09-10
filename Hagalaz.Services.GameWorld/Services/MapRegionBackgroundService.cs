using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    public class MapRegionBackgroundService : BackgroundService
    {
        private readonly IMapRegionService _regionService;
        private readonly ILogger<MapRegionBackgroundService> _logger;

        public MapRegionBackgroundService(IMapRegionService regionService, ILogger<MapRegionBackgroundService> logger)
        {
            _regionService = regionService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    await ProcessRegionsOnceAsync();
                }
                catch (TaskCanceledException)
                {
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Failed to service regions");
                }
            }
        }

        internal async Task ProcessRegionsOnceAsync()
        {
            foreach (var dimension in _regionService.FindAllDimensions())
            {
                var regionsToDestroy = new List<IMapRegion>();

                foreach (var region in dimension.Regions.Values
                             .Where(region => region.State == MapRegionState.Ready && region.CanSuspend()))
                {
                    if (_regionService.TrySuspendMapRegion(region))
                    {
                        _logger.LogDebug("Region[{id}] was suspended.", region.Id);
                    }
                }

                foreach (var region in dimension.IdleRegions.Values.Where(region => region.CanDestroy()))
                {
                    if (!_regionService.TryTakeIdleMapRegionForDestroy(region.Id, dimension.Id, region))
                    {
                        continue;
                    }

                    regionsToDestroy.Add(region);
                }

                foreach (var region in _regionService.FindPendingDestructionRegions(dimension.Id))
                {
                    if (!regionsToDestroy.Contains(region, ReferenceEqualityComparer.Instance))
                    {
                        regionsToDestroy.Add(region);
                    }
                }

                foreach (var region in regionsToDestroy)
                {
                    try
                    {
                        await region.DestroyAsync();
                        _regionService.TryCompleteMapRegionDestruction(region);
                        _logger.LogDebug("Region[{id}] was destroyed.", region.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to destroy pending region[{id}] in dimension[{dimension}]; it remains owned for retry.", region.Id, dimension.Id);
                    }
                }

                if (dimension.CanDestroy()
                    && _regionService.TryRemoveEmptyDimension(dimension))
                {
                    _logger.LogDebug("Dimension[{id}] was destroyed.", dimension.Id);
                }
            }
        }
    }
}
