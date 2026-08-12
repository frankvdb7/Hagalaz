using Hagalaz.Game.Messages;
using Hagalaz.Services.Contacts.Store;
using MassTransit;

namespace Hagalaz.Services.Contacts.Services
{
    public class WorldStatusService : IHostedService
    {
        private readonly IBus _bus;
        private readonly ILogger<WorldStatusService> _logger;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly WorldSessionStore _worldSessions;
        private readonly WorldContactCleanupService _cleanupService;
        private readonly CancellationTokenSource _stopping = new();
        private Task? _runTask;

        public WorldStatusService(
            IBus bus,
            ILogger<WorldStatusService> logger,
            IHostApplicationLifetime applicationLifetime,
            WorldSessionStore worldSessions,
            WorldContactCleanupService cleanupService)
        {
            _bus = bus;
            _logger = logger;
            _applicationLifetime = applicationLifetime;
            _worldSessions = worldSessions;
            _cleanupService = cleanupService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _applicationLifetime.ApplicationStarted.Register(() =>
            {
                _runTask = RunAsync(_stopping.Token);
            });

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _stopping.Cancel();
            if (_runTask == null)
            {
                _stopping.Dispose();
                return;
            }

            try
            {
                await _runTask.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || _stopping.IsCancellationRequested)
            {
                // The service is stopping; no further discovery or cleanup work is required.
            }
            finally
            {
                _stopping.Dispose();
            }
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _bus.Publish(new WorldStatusRequest(), cancellationToken);
                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    foreach (var update in _worldSessions.Expire())
                    {
                        if (!update.IsAvailable)
                        {
                            await _cleanupService.SignOutWorldContactsAsync(update.WorldId, _bus, cancellationToken);
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Normal hosted-service shutdown.
            }
            catch (MassTransitException exception)
            {
                _logger.LogError(exception, "Error while requesting world status");
            }
            catch (TimeoutException exception)
            {
                _logger.LogError(exception, "Timed out while requesting world status");
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogError(exception, "Invalid world status state");
            }
        }
    }
}
