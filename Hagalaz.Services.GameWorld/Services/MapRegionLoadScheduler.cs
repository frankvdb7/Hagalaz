using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Metrics;
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
        private readonly Dictionary<IMapRegion, TaskCompletionSource<bool>> _dynamicPopulations = new(ReferenceEqualityComparer.Instance);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MapRegionLoadScheduler> _logger;
        private readonly TimeProvider _timeProvider;
        private readonly Channel<(IMapRegion Region, long EnqueuedAt)> _requests = Channel.CreateUnbounded<(IMapRegion Region, long EnqueuedAt)>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        private bool _stopping;

        public MapRegionLoadScheduler(
            IServiceScopeFactory scopeFactory,
            ILogger<MapRegionLoadScheduler> logger,
            TimeProvider? timeProvider = null)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _timeProvider = timeProvider ?? TimeProvider.System;
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
            await foreach (var (region, enqueuedAt) in _requests.Reader.ReadAllAsync(stoppingToken))
            {
                if (enqueuedAt != 0)
                    GameWorldMetrics.RecordRegionLoadQueueDuration(_timeProvider.GetElapsedTime(enqueuedAt));
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
                if (region.IsDynamic)
                {
                    // A dynamic region is populated by its creator, never by the normal loader.
                    if (_dynamicPopulations.TryGetValue(region, out var population))
                        return AwaitPopulationAsync(region, population.Task, CancellationToken.None);
                    if (tolerateInvalidState)
                        return null;
                    throw new InvalidOperationException($"Dynamic region {region.Id} is not being populated and cannot be loaded.");
                }

                if (_inFlight.TryGetValue(region, out var completion))
                    return completion.Task;

                completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _inFlight.Add(region, completion);
                var enqueuedAt = GameWorldMetrics.RegionLoadQueueDurationEnabled ? _timeProvider.GetTimestamp() : 0;
                if (_requests.Writer.TryWrite((region, enqueuedAt)))
                {
                    GameWorldMetrics.RegionLoadQueued();
                    return completion.Task;
                }

                _inFlight.Remove(region);
                completion.TrySetException(new InvalidOperationException($"Unable to schedule loading for region {region.Id}."));
                throw new InvalidOperationException($"Unable to schedule loading for region {region.Id}.");
            }
        }

        public void BeginDynamicPopulation(IMapRegion region)
        {
            ArgumentNullException.ThrowIfNull(region);
            lock (_stateLock)
                _dynamicPopulations[region] = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public void CompleteDynamicPopulation(IMapRegion region, bool succeeded)
        {
            TaskCompletionSource<bool>? population;
            lock (_stateLock)
                _dynamicPopulations.Remove(region, out population);
            population?.TrySetResult(succeeded);
        }

        public Task WaitForDynamicPopulationAsync(IMapRegion region, CancellationToken cancellationToken = default)
        {
            TaskCompletionSource<bool>? population;
            lock (_stateLock)
                _dynamicPopulations.TryGetValue(region, out population);
            return population is null ? Task.CompletedTask : AwaitPopulationAsync(region, population.Task, cancellationToken);
        }

        private static async Task AwaitPopulationAsync(IMapRegion region, Task<bool> population, CancellationToken cancellationToken)
        {
            if (!await population.WaitAsync(cancellationToken))
                throw new InvalidOperationException($"Dynamic region {region.Id} failed to populate.");
        }

        private void Complete(IMapRegion region, bool cancellation, Exception? exception)
        {
            TaskCompletionSource? completion;
            lock (_stateLock)
                _inFlight.Remove(region, out completion);

            if (completion is null)
                return;
            GameWorldMetrics.RegionLoadsFinished(1);
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
            List<TaskCompletionSource<bool>> populations;
            lock (_stateLock)
            {
                _stopping = true;
                populations = _dynamicPopulations.Values.ToList();
                _dynamicPopulations.Clear();
                completions = _inFlight.Values.ToList();
                GameWorldMetrics.RegionLoadsFinished(_inFlight.Count);
                _inFlight.Clear();
            }

            _requests.Writer.TryComplete();
            foreach (var completion in completions)
                completion.TrySetCanceled();
            foreach (var population in populations)
                population.TrySetCanceled();

            await base.StopAsync(stoppingToken);
        }
    }
}
