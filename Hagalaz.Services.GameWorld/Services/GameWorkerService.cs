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
            await base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tickTimeSpan = _gameOptions.TickTimeSpan;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(tickTimeSpan, stoppingToken);

                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }

                    // Execute 'major-update' tasks.
                    _rsTaskScheduler.Tick();

                    var stopwatch = Stopwatch.StartNew();
                    try
                    {
                        await RunMajorTickAsync();
                    }
                    finally
                    {
                        stopwatch.Stop();
                        if (stopwatch.Elapsed > tickTimeSpan)
                        {
                            _logger.LogWarning(
                                "Major game tick exceeded its configured budget. Elapsed: {Elapsed}; budget: {Budget}.",
                                stopwatch.Elapsed,
                                tickTimeSpan);
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in major game tick.");
                }
            }
        }

        private async Task RunMajorTickAsync()
        {
            var regions = _regionService.FindAllRegions().ToList();
            foreach (var region in regions)
            {
                await region.MajorUpdateTick();
            }

            foreach (var region in regions)
            {
                await region.MajorClientPrepareUpdateTick();
            }

            foreach (var region in regions)
            {
                await region.MajorClientUpdateTick();
            }

            foreach (var region in regions)
            {
                await region.MajorClientUpdateResetTick();
            }
        }
    }
}
