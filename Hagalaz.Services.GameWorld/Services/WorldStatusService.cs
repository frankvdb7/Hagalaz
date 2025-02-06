using System;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Messages;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hagalaz.Services.GameWorld.Services
{
    public class WorldStatusService : IHostedService
    {
        private readonly IBus _publishEndpoint;
        private readonly IGameMediator _mediator;
        private readonly IOptions<WorldOptions> _worldOptions;
        private readonly ILogger<WorldStatusService> _logger;
        private readonly IHostApplicationLifetime _applicationLifetime;

        public WorldStatusService(
            IBus publishEndpoint, IGameMediator mediator, IOptions<WorldOptions> worldOptions, ILogger<WorldStatusService> logger,
            IHostApplicationLifetime applicationLifetime)
        {
            _publishEndpoint = publishEndpoint;
            _mediator = mediator;
            _worldOptions = worldOptions;
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
                        var worldOnlineMessage = await _mediator.GetResponseAsync<WorldStatusRequest, WorldOnlineMessage>(new WorldStatusRequest());
                        await _publishEndpoint.Publish(worldOnlineMessage);
                        _logger.LogInformation("{Name} started successfully.", nameof(WorldStatusService));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while publishing publishing world online message");
                    }
                });
            });
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Name} is stopping...", nameof(WorldStatusService));
            try
            {
                var options = _worldOptions.Value;
                await _publishEndpoint.Publish(new WorldOfflineMessage(options.Id), cancellationToken);
                _logger.LogInformation("{Name} stopped successfully", nameof(WorldStatusService));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while publishing {type}", nameof(WorldOfflineMessage));
            }
        }
    }
}