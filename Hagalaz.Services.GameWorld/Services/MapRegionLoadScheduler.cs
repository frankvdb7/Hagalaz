using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Services
{
    /// <summary>
    /// Owns asynchronous map-region load requests independently from the synchronous game tick.
    /// </summary>
    public sealed class MapRegionLoadScheduler : BackgroundService, IMapRegionLoadScheduler
    {
        private readonly Channel<IMapRegion> _requests = Channel.CreateUnbounded<IMapRegion>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        private readonly ConcurrentDictionary<IMapRegion, byte> _scheduled = new();
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MapRegionLoadScheduler> _logger;
        private volatile bool _stopping;

        public MapRegionLoadScheduler(
            IServiceScopeFactory scopeFactory,
            ILogger<MapRegionLoadScheduler> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public void RequestLoad(IMapRegion region)
        {
            if (_stopping || region.IsLoaded || !_scheduled.TryAdd(region, 0))
            {
                return;
            }

            if (_requests.Writer.TryWrite(region))
            {
                return;
            }

            _scheduled.TryRemove(region, out _);

            if (_stopping)
            {
                return;
            }

            throw new InvalidOperationException($"Unable to schedule loading for region {region.Id}.");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var region in _requests.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<IMapRegionLoader>()
                        .LoadAsync(region, stoppingToken);
                }
                catch (OperationCanceledException ex) when (stoppingToken.IsCancellationRequested && ex.CancellationToken == stoppingToken)
                {
                    _logger.LogDebug(ex, "Loading region {id} was canceled during scheduler shutdown", region.Id);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Failed to load region {id}", region.Id);
                }
                finally
                {
                    _scheduled.TryRemove(region, out _);
                }
            }
        }

        public override Task StopAsync(CancellationToken stoppingToken)
        {
            _stopping = true;
            _requests.Writer.TryComplete();
            return base.StopAsync(stoppingToken);
        }
    }
}
