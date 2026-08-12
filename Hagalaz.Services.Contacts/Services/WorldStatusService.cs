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
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly CancellationTokenSource _stopping = new();
        private Task? _runTask;

        public WorldStatusService(
            IBus bus,
            ILogger<WorldStatusService> logger,
            IHostApplicationLifetime applicationLifetime,
            WorldSessionStore worldSessions,
            IServiceScopeFactory scopeFactory)
        {
            _bus = bus;
            _logger = logger;
            _applicationLifetime = applicationLifetime;
            _worldSessions = worldSessions;
            _scopeFactory = scopeFactory;
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
            using var stopping = _stopping;
            stopping.Cancel();
            if (_runTask == null)
            {
                return;
            }

            try
            {
                await _runTask.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || stopping.IsCancellationRequested)
            {
                // The service is stopping; no further discovery or cleanup work is required.
            }
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                await TryRequestWorldStatusAsync(cancellationToken);
                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    foreach (var update in _worldSessions.Expire())
                    {
                        if (!update.IsAvailable)
                        {
                            await TryRemoveWorldSessionsAsync(update.WorldId, cancellationToken);
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

        private async Task TryRequestWorldStatusAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _bus.Publish(new WorldStatusRequest(), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException exception)
            {
                _logger.LogWarning(exception, "World status request was cancelled before completion");
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

        private async Task TryRemoveWorldSessionsAsync(int worldId, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var contactSessionService = scope.ServiceProvider.GetRequiredService<IContactSessionService>();
                await contactSessionService.RemoveWorldSessions(worldId);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException exception)
            {
                _logger.LogWarning(exception, "Contact cleanup was cancelled before completion for world {WorldId}", worldId);
            }
            catch (MassTransitException exception)
            {
                _logger.LogError(exception, "Error while cleaning up contacts for world {WorldId}", worldId);
            }
            catch (TimeoutException exception)
            {
                _logger.LogError(exception, "Timed out while cleaning up contacts for world {WorldId}", worldId);
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogError(exception, "Invalid contact cleanup state for world {WorldId}", worldId);
            }
        }
    }
}
