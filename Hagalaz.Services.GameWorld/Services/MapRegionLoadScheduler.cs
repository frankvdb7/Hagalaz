using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        private readonly object _stateLock = new();
        private readonly Dictionary<IMapRegion, TaskCompletionSource> _inFlight = new();
        private readonly IMapRegionService _regionService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MapRegionLoadScheduler> _logger;
        private readonly MapRegionLoadRequestQueue _requests;
        private bool _stopping;

        public MapRegionLoadScheduler(
            IMapRegionService regionService,
            IServiceScopeFactory scopeFactory,
            ILogger<MapRegionLoadScheduler> logger,
            MapRegionLoadRequestQueue requests)
        {
            _regionService = regionService;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _requests = requests;
        }

        public MapRegionLoadScheduler(
            IMapRegionService regionService,
            IServiceScopeFactory scopeFactory,
            ILogger<MapRegionLoadScheduler> logger)
            : this(regionService, scopeFactory, logger, new MapRegionLoadRequestQueue())
        {
        }

        public void RequestLoad(IMapRegion region) => _ = GetOrRequestLoad(region, true);

        public async Task EnsureLoadedAsync(IEnumerable<IMapRegion> regions, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(regions);
            var waits = new List<Task>();
            foreach (var region in regions.Distinct())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var completion = GetOrRequestLoad(region, false);
                if (completion is not null)
                    waits.Add(completion);
            }

            await Task.WhenAll(waits).WaitAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var region in _requests.ReadAllAsync(stoppingToken))
            {
                if (!TryClaimQueuedRegion(region))
                    continue;

                Exception? failure = null;
                var canceled = false;
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<IMapRegionLoader>().LoadAsync(region, stoppingToken);
                    if (region.State != MapRegionState.Ready)
                        throw new InvalidOperationException($"Region {region.Id} did not publish readiness after loading.");
                }
                catch (OperationCanceledException ex) when (stoppingToken.IsCancellationRequested && ex.CancellationToken == stoppingToken)
                {
                    failure = ex;
                    canceled = true;
                    _logger.LogDebug(ex, "Loading region {id} was canceled during scheduler shutdown", region.Id);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failure = ex;
                    _logger.LogError(ex, "Failed to load region {id}", region.Id);
                }
                catch (OperationCanceledException ex)
                {
                    failure = ex;
                    canceled = true;
                }
                finally
                {
                    Complete(region, canceled, failure);
                }
            }
        }

        private Task? GetOrRequestLoad(IMapRegion region, bool tolerateInvalidState)
        {
            lock (_stateLock)
            {
                if (_stopping)
                {
                    if (tolerateInvalidState)
                        return null;
                    throw new InvalidOperationException("The map-region scheduler is stopping.");
                }

                if (region.State == MapRegionState.Ready)
                    return null;
                if (region.State == MapRegionState.Discarded)
                {
                    if (tolerateInvalidState)
                        return null;
                    throw new InvalidOperationException($"Region {region.Id} is discarded and cannot be loaded.");
                }
                if (!IsCurrent(region))
                {
                    if (tolerateInvalidState)
                        return null;
                    throw new InvalidOperationException($"Region {region.Id} is no longer the current region instance.");
                }
                if (_inFlight.TryGetValue(region, out var completion))
                    return completion.Task;

                completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _inFlight.Add(region, completion);
                if (_requests.TryRequestLoad(region))
                    return completion.Task;

                _inFlight.Remove(region);
                completion.TrySetException(new InvalidOperationException($"Unable to schedule loading for region {region.Id}."));
                throw new InvalidOperationException($"Unable to schedule loading for region {region.Id}.");
            }
        }

        private bool TryClaimQueuedRegion(IMapRegion region)
        {
            lock (_stateLock)
            {
                if (_stopping || region.State is MapRegionState.Ready or MapRegionState.Discarded || !IsCurrent(region))
                    return false;
                if (_inFlight.ContainsKey(region))
                    return true;

                _inFlight.Add(region, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
                return true;
            }
        }

        private bool IsCurrent(IMapRegion region) =>
            _regionService.IsCurrentMapRegion(region.Id, region.BaseLocation.Dimension, region);

        private void Complete(IMapRegion region, bool cancellation, Exception? exception)
        {
            TaskCompletionSource? completion;
            lock (_stateLock)
                _inFlight.Remove(region, out completion);

            if (completion is null)
                return;
            if (cancellation)
                completion.TrySetCanceled();
            else if (exception is null)
                completion.TrySetResult();
            else
                completion.TrySetException(exception);
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            List<TaskCompletionSource> completions;
            lock (_stateLock)
            {
                _stopping = true;
                completions = _inFlight.Values.ToList();
            }

            _requests.Complete();
            foreach (var completion in completions)
                completion.TrySetCanceled();

            await base.StopAsync(stoppingToken);
        }
    }
}
