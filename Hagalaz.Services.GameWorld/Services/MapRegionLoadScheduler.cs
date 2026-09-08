using System;
using System.Collections.Concurrent;
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
        private readonly ConcurrentDictionary<IMapRegion, byte> _scheduled = new();
        private readonly ConcurrentDictionary<IMapRegion, TaskCompletionSource<Exception?>> _completions = new();
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
            if (TryQueueRegion(region, out _) || _stopping)
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

            var waits = new List<Task<Exception?>>();
            foreach (var region in regions.Distinct())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (region.IsLoaded)
                {
                    continue;
                }

                if (_stopping)
                {
                    throw new InvalidOperationException("The map-region scheduler is stopping.");
                }

                if (!TryQueueRegion(region, out var completion))
                {
                    if (_stopping)
                    {
                        throw new InvalidOperationException("The map-region scheduler is stopping.");
                    }

                    throw new InvalidOperationException($"Unable to schedule loading for region {region.Id}.");
                }

                if (completion != null)
                {
                    waits.Add(completion.Task);
                }
            }

            var pending = waits.ToHashSet();
            while (pending.Count > 0)
            {
                var completed = await Task.WhenAny(pending).WaitAsync(cancellationToken);
                pending.Remove(completed);
                var failure = await completed;
                if (failure == null)
                {
                    continue;
                }

                if (failure is OperationCanceledException operationCanceledException)
                {
                    throw operationCanceledException;
                }

                throw new InvalidOperationException("One or more visible map regions failed to become ready.", failure);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var region in _requests.Reader.ReadAllAsync(stoppingToken))
            {
                Exception? failure = null;
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<IMapRegionLoader>()
                        .LoadAsync(region, stoppingToken);
                    if (!region.IsLoaded)
                    {
                        failure = new InvalidOperationException($"Region {region.Id} did not publish readiness after loading.");
                    }
                }
                catch (OperationCanceledException ex) when (stoppingToken.IsCancellationRequested && ex.CancellationToken == stoppingToken)
                {
                    failure = ex;
                    _logger.LogDebug(ex, "Loading region {id} was canceled during scheduler shutdown", region.Id);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failure = ex;
                    _logger.LogError(ex, "Failed to load region {id}", region.Id);
                }
                finally
                {
                    TaskCompletionSource<Exception?>? completion;
                    lock (_stateLock)
                    {
                        _scheduled.TryRemove(region, out _);
                        _completions.TryRemove(region, out completion);
                    }

                    completion?.TrySetResult(failure);
                }
            }
        }

        private bool TryQueueRegion(IMapRegion region, out TaskCompletionSource<Exception?>? completion)
        {
            lock (_stateLock)
            {
                if (_stopping)
                {
                    completion = null;
                    return false;
                }

                if (region.IsLoaded)
                {
                    completion = null;
                    return true;
                }

                completion = _completions.GetOrAdd(region, static _ =>
                    new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously));
                if (!_scheduled.TryAdd(region, 0))
                {
                    return true;
                }

                if (_requests.Writer.TryWrite(region))
                {
                    return true;
                }

                _scheduled.TryRemove(region, out _);
                _completions.TryRemove(region, out _);
                completion.TrySetResult(new InvalidOperationException($"Unable to schedule loading for region {region.Id}."));
                return false;
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
