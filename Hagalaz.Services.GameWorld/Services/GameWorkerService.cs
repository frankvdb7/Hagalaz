using System;
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
    public class GameWorkerService : IHostedService, IDisposable
    {
        private readonly IRsTaskService _rsTaskScheduler;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IMapRegionService _regionService;
        private readonly GameServerOptions _gameOptions;
        private readonly ILogger<GameWorkerService> _logger;

        public GameWorkerService(IRsTaskService rsTaskScheduler, IMapRegionService regionService, IOptions<GameServerOptions> gameOptions, ILogger<GameWorkerService> logger)
        {
            _rsTaskScheduler = rsTaskScheduler;
            _regionService = regionService;
            _gameOptions = gameOptions.Value;
            _logger = logger;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Name} is starting.", nameof(GameWorkerService));
            cancellationToken.Register(() => _cancellationTokenSource.Cancel());
            _ = Task.Run(DoWork, _cancellationTokenSource.Token);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Name} is stopping.", nameof(GameWorkerService));
            _cancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }

        private async Task DoWork()
        {
            var tickTimeSpan = TimeSpan.FromMilliseconds(_gameOptions.TickTimeSpan.TotalMilliseconds);
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(tickTimeSpan, _cancellationTokenSource.Token);

                    // Execute 'major-update' tasks.
                    _rsTaskScheduler.Tick();

                    async Task Tick()
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

                    // throw timeout exception if a tick takes too long
                    await Tick().WaitAsync(tickTimeSpan, _cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in major game tick.");
                }
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
    }
}