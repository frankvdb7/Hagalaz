using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly object _stateLock = new();
        private readonly Dictionary<IMapRegion, TaskCompletionSource> _inFlight = new();
        private readonly IMapRegionService _regionService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MapRegionLoadScheduler> _logger;
        private bool _stopping;

        public MapRegionLoadScheduler(
            IMapRegionService regionService,
            IServiceScopeFactory scopeFactory,
            ILogger<MapRegionLoadScheduler> logger)
        {
            _regionService = regionService;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public void RequestLoad(IMapRegion region)
        {
            if (TryQueueRegion(region, out _) || IsStopping)
            {
                return;
            }

            if (region.State is MapRegionState.Ready or MapRegionState.Discarded || !IsCurrent(region))
            {
                return;
            }

            throw new InvalidOperationException($"Unable to schedule loading for region {region.Id}.");
        }

        /// <summary>
        /// Waits for one load attempt for each region that is not already ready.
        /// This is intentionally internal so script-facing map APIs remain synchronous.
        /// </summary>
        internal async Task EnsureLoadedAsync(IEnumerable<IMapRegion> regions, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(regions);

            var waits = new List<Task>();
            foreach (var region in regions.Distinct())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (region.State == MapRegionState.Ready)
                {
                    continue;
                }

                if (region.State == MapRegionState.Discarded)
                {
                    throw new InvalidOperationException($"Region {region.Id} is discarded and cannot be loaded.");
                }

                if (!IsCurrent(region))
                {
                    throw new InvalidOperationException($"Region {region.Id} is no longer the current region instance.");
                }

                if (IsStopping)
                {
                    throw new InvalidOperationException("The map-region scheduler is stopping.");
                }

                if (!TryQueueRegion(region, out var completion))
                {
                    if (IsStopping)
                    {
                        throw new InvalidOperationException("The map-region scheduler is stopping.");
                    }

                    throw new InvalidOperationException($"Unable to schedule loading for region {region.Id}.");
                }

                if (completion != null)
                    waits.Add(completion.Task);
            }

            await Task.WhenAll(waits).WaitAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var region in _requests.Reader.ReadAllAsync(stoppingToken))
            {
                Exception? failure = null;
                var canceled = false;
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<IMapRegionLoader>()
                        .LoadAsync(region, stoppingToken);
                    if (region.State != MapRegionState.Ready)
                    {
                        throw new InvalidOperationException($"Region {region.Id} did not publish readiness after loading.");
                    }
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

        private bool TryQueueRegion(IMapRegion region, out TaskCompletionSource? completion)
        {
            lock (_stateLock)
            {
                if (_stopping)
                {
                    completion = null;
                    return false;
                }

                if (region.State == MapRegionState.Ready)
                {
                    completion = null;
                    return true;
                }

                if (region.State == MapRegionState.Discarded || !IsCurrent(region))
                {
                    completion = null;
                    return false;
                }

                if (_inFlight.TryGetValue(region, out completion))
                {
                    return true;
                }

                completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _inFlight.Add(region, completion);
                if (_requests.Writer.TryWrite(region))
                {
                    return true;
                }

                _inFlight.Remove(region);
                completion.TrySetException(new InvalidOperationException($"Unable to schedule loading for region {region.Id}."));
                return false;
            }
        }

        private bool IsCurrent(IMapRegion region) =>
            _regionService.IsCurrentMapRegion(region.Id, region.BaseLocation.Dimension, region);

        private bool IsStopping
        {
            get
            {
                lock (_stateLock)
                {
                    return _stopping;
                }
            }
        }

        private void Complete(IMapRegion region, bool cancellation, Exception? exception)
        {
            TaskCompletionSource? completion;
            lock (_stateLock)
            {
                _inFlight.Remove(region, out completion);
            }

            if (completion is null)
                return;

            if (cancellation)
            {
                completion.TrySetCanceled();
            }
            else if (exception is null)
            {
                completion.TrySetResult();
            }
            else
            {
                completion.TrySetException(exception);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            List<TaskCompletionSource> completions;
            lock (_stateLock)
            {
                _stopping = true;
                completions = _inFlight.Values.ToList();
            }

            _requests.Writer.TryComplete();
            foreach (var completion in completions)
            {
                completion.TrySetCanceled();
            }

            await base.StopAsync(stoppingToken);
        }
    }
}
