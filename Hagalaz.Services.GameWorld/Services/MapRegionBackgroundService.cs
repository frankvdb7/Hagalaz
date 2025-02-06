using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
                    foreach (var dimension in _regionService.FindAllDimensions())
                    {
                        var activeRegions = dimension.Regions.Values;
                        foreach (var region in activeRegions.Where(region => region.CanSuspend()))
                        {
                            dimension.Regions.Remove(region.Id);
                            region.Suspend();
                            dimension.IdleRegions.Add(region.Id, region);
                            _logger.LogDebug("Region[{id}] was suspended.", region.Id);
                        }

                        var idleRegions = dimension.IdleRegions.Values;
                        foreach (var region in idleRegions.Where(region => region.CanDestroy()))
                        {
                            await region.DestroyAsync();
                            dimension.IdleRegions.Remove(region.Id);
                            _logger.LogDebug("Region[{id}] was destroyed.", region.Id);
                        }

                        if (dimension.CanDestroy())
                        {
                            _regionService.RemoveDimension(dimension);
                            _logger.LogDebug("Dimension[{id}] was destroyed.", dimension.Id);
                        }
                    }                    
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
    }
}
