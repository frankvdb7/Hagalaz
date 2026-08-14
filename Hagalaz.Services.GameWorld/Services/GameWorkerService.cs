using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hagalaz.Services.GameWorld.Services
{
    public class GameWorkerService : BackgroundService
    {
        private readonly IRsTaskService _rsTaskScheduler;
        private readonly IMapRegionService _regionService;
        private readonly GameServerOptions _gameOptions;
        private readonly ILogger<GameWorkerService> _logger;

        public GameWorkerService(
            IRsTaskService rsTaskScheduler,
            IMapRegionService regionService,
            IOptions<GameServerOptions> gameOptions,
            ILogger<GameWorkerService> logger)
        {
            _rsTaskScheduler = rsTaskScheduler;
            _regionService = regionService;
            _gameOptions = gameOptions.Value;
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Name} is starting.", nameof(GameWorkerService));
            return base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Name} is stopping.", nameof(GameWorkerService));

            // Region APIs can only observe cancellation at explicit safe boundaries. Do not let
            // the host shutdown timeout make BackgroundService report a successful stop while a
            // worker-owned region operation is still running.
            await base.StopAsync(CancellationToken.None);
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
                    // Execute 'major-update' tasks.
                    _rsTaskScheduler.Tick();

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
                        if (tickCompleted && stopwatch.Elapsed > tickTimeSpan)
                        {
                            _logger.LogWarning(
                                "Major game tick exceeded its configured budget. Elapsed: {Elapsed}; budget: {Budget}.",
                                stopwatch.Elapsed,
                                tickTimeSpan);
                        }
                    }
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

        private async Task RunMajorTickAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();
            var regions = _regionService.FindAllRegions().ToList();
            foreach (var region in regions)
            {
                stoppingToken.ThrowIfCancellationRequested();
                await region.MajorUpdateTick(stoppingToken);
            }

            foreach (var region in regions)
            {
                stoppingToken.ThrowIfCancellationRequested();
                await region.MajorClientPrepareUpdateTick(stoppingToken);
            }

            foreach (var region in regions)
            {
                stoppingToken.ThrowIfCancellationRequested();
                await region.MajorClientUpdateTick(stoppingToken);
            }

            foreach (var region in regions)
            {
                stoppingToken.ThrowIfCancellationRequested();
                await region.MajorClientUpdateResetTick(stoppingToken);
            }
        }
    }
}
