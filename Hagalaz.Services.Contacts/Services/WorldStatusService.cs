using Hagalaz.Game.Messages;
using MassTransit;

namespace Hagalaz.Services.Contacts.Services
{
    public class WorldStatusService : IHostedService
    {
        private readonly IBus _bus;
        private readonly ILogger<WorldStatusService> _logger;
        private readonly IHostApplicationLifetime _applicationLifetime;

        public WorldStatusService(IBus bus, ILogger<WorldStatusService> logger, IHostApplicationLifetime applicationLifetime)
        {
            _bus = bus;
            _logger = logger;
            _applicationLifetime = applicationLifetime;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            _applicationLifetime.ApplicationStarted.Register(() =>
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _bus.Publish(new WorldStatusRequest());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while trying to request world status");
                    }
                });
            });
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}