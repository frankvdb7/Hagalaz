using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hagalaz.Services.GameWorld.Services
{
    public class GameWorkerService : BackgroundService, IGameWorkerExecution
    {
        private readonly IRsTaskService _rsTaskScheduler;
        private readonly IMapRegionService _regionService;
        private readonly MapRegionBackgroundService _regionHousekeeping;
        private readonly ICharacterStore _characterStore;
        private readonly GameServerOptions _gameOptions;
        private readonly ILogger<GameWorkerService> _logger;
        private readonly TimeProvider _timeProvider;
        private long? _previousTickStart;
        private readonly TaskCompletionSource _executionCompleted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task ExecutionCompleted => _executionCompleted.Task;

        public GameWorkerService(
            IRsTaskService rsTaskScheduler,
            IMapRegionService regionService,
            MapRegionBackgroundService regionHousekeeping,
            ICharacterStore characterStore,
            IOptions<GameServerOptions> gameOptions,
            ILogger<GameWorkerService> logger,
            TimeProvider timeProvider)
        {
            _rsTaskScheduler = rsTaskScheduler;
            _regionService = regionService;
            _regionHousekeeping = regionHousekeeping;
            _characterStore = characterStore;
            _gameOptions = gameOptions.Value;
            _logger = logger;
            _timeProvider = timeProvider;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Name} is starting.", nameof(GameWorkerService));
            var startTask = base.StartAsync(cancellationToken);
            if (ExecuteTask is { } executionTask)
            {
                _ = executionTask.ContinueWith(
                    _ => _executionCompleted.TrySetResult(),
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
            else
            {
                _executionCompleted.TrySetResult();
            }

            return startTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Name} is stopping.", nameof(GameWorkerService));
            await base.StopAsync(cancellationToken);

            if (ExecuteTask is { IsCompleted: false })
            {
                _logger.LogWarning("Host shutdown ended before the game worker completed its synchronous tick.");
                throw new TimeoutException("The game worker is still executing after the host shutdown timeout.");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tickTimeSpan = _gameOptions.TickTimeSpan;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(tickTimeSpan, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    await ExecuteTickAsync(stoppingToken);
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == stoppingToken)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in major game tick.");
                }
            }
        }

        internal async Task ExecuteTickAsync(CancellationToken stoppingToken)
        {
            if (!GameWorldMetrics.TickMetricsEnabled)
            {
                _previousTickStart = null;
                await RunTickAsync(stoppingToken);
                return;
            }

            var start = _timeProvider.GetTimestamp();
            if (_previousTickStart is { } previousStart)
            {
                GameWorldMetrics.RecordTickInterval(_timeProvider.GetElapsedTime(previousStart, start));
            }

            _previousTickStart = start;
            var outcome = TickOutcome.Failure;
            try
            {
                await RunTickAsync(stoppingToken);
                outcome = TickOutcome.Success;
            }
            catch (OperationCanceledException ex) when (stoppingToken.IsCancellationRequested && ex.CancellationToken == stoppingToken)
            {
                outcome = TickOutcome.Cancelled;
                throw;
            }
            finally
            {
                var elapsed = _timeProvider.GetElapsedTime(start);
                GameWorldMetrics.RecordTick(outcome, elapsed, elapsed > _gameOptions.TickTimeSpan);
            }
        }

        private async Task RunTickAsync(CancellationToken stoppingToken)
        {
            // Execute 'major-update' tasks.
            using (GameWorldMetrics.BeginTickPhase(TickPhase.Scheduler, _timeProvider))
            {
                _rsTaskScheduler.Tick();
            }

            var stopwatch = Stopwatch.StartNew();
            var tickCompleted = false;
            try
            {
                await RunMajorTickAsync(stoppingToken);
                tickCompleted = true;
            }
            finally
            {
                stopwatch.Stop();
                if (tickCompleted && stopwatch.Elapsed > _gameOptions.TickTimeSpan)
                {
                    _logger.LogWarning(
                        "Major game tick exceeded its configured budget. Elapsed: {Elapsed}; budget: {Budget}.",
                        stopwatch.Elapsed,
                        _gameOptions.TickTimeSpan);
                }
            }
        }

        private async Task RunMajorTickAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();
            var regions = _regionService.FindReadyRegions();
            GameWorldMetrics.RecordTickRegions(regions.Count);
            var characters = await _characterStore.GetSnapshotAsync(stoppingToken);

            stoppingToken.ThrowIfCancellationRequested();
            var simulationFailed = true;
            try
            {
                using (GameWorldMetrics.BeginTickPhase(TickPhase.MajorUpdate, _timeProvider))
                {
                    foreach (var region in regions)
                    {
                        region.MajorUpdateTick();
                    }
                }

                using (GameWorldMetrics.BeginTickPhase(TickPhase.ClientPrepare, _timeProvider))
                {
                    foreach (var region in regions)
                    {
                        region.MajorClientPrepareUpdateTick();
                    }
                }

                using (GameWorldMetrics.BeginTickPhase(TickPhase.ClientUpdate, _timeProvider))
                {
                    foreach (var region in regions)
                    {
                        region.MajorClientUpdateTick(characters);
                    }
                }

                simulationFailed = false;
            }
            finally
            {
                ResetRegions(regions, simulationFailed);
            }

            using (GameWorldMetrics.BeginTickPhase(TickPhase.Housekeeping, _timeProvider))
            {
                await _regionHousekeeping.ProcessRegionsIfDueAsync(characters);
            }

        }

        /// <summary>
        /// Resets every region's per-tick client state. A failing region never prevents the remaining regions from
        /// being reset; reset failures are logged, and only surface when they would not mask a simulation failure.
        /// </summary>
        private void ResetRegions(IReadOnlyCollection<IMapRegion> regions, bool simulationFailed)
        {
            List<Exception>? failures = null;
            using (GameWorldMetrics.BeginTickPhase(TickPhase.ClientReset, _timeProvider))
            {
                foreach (var region in regions)
                {
                    try
                    {
                        region.MajorClientUpdateResetTick();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred while resetting a map region's client update state.");
                        (failures ??= []).Add(ex);
                    }
                }
            }

            if (failures is null || simulationFailed)
            {
                return;
            }

            if (failures.Count == 1)
            {
                ExceptionDispatchInfo.Capture(failures[0]).Throw();
            }

            throw new AggregateException(failures);
        }
    }
}
