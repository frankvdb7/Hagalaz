using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
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
                    await DestroyDetachedRegionAsync(region);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
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

        internal async Task DestroyDetachedRegionsAsync(CancellationToken cancellationToken = default)
        {
            while (_detachedRegions.Reader.TryRead(out var region))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await DestroyDetachedRegionAsync(region);
            }
        }

        internal Task ProcessRegionsOnceAsync()
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

                    _detachedRegions.Writer.TryWrite(region);
                }

                if (_regionService.TryRemoveEmptyDimension(dimension))
                {
                    _logger.LogDebug("Dimension[{id}] was destroyed.", dimension.Id);
                }
            }

            return Task.CompletedTask;
        }

        private async Task DestroyDetachedRegionAsync(IMapRegion region)
        {
            try
            {
                await region.DestroyAsync();
                _logger.LogDebug("Region[{id}] was destroyed.", region.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to destroy detached region[{id}]; its ownership was already released.", region.Id);
            }
        }
    }
}
