using System.Threading.Tasks;
using Hagalaz.Game.Messages;
using Hagalaz.Services.Contacts.Services;
using Hagalaz.Services.Contacts.Store;
using Hagalaz.Services.Contacts.Store.Model;
using MassTransit;

namespace Hagalaz.Services.Contacts.Consumers
{
    public class WorldStatusConsumer : IConsumer<WorldOnlineMessage>, IConsumer<WorldOfflineMessage>
    {
        private readonly WorldSessionStore _worldSessions;
        private readonly IContactSessionService _contactSessionService;
        private readonly ILogger<WorldStatusConsumer> _logger;

        public WorldStatusConsumer(
            WorldSessionStore worldSessions,
            IContactSessionService contactSessionService,
            ILogger<WorldStatusConsumer> logger)
        {
            _worldSessions = worldSessions;
            _contactSessionService = contactSessionService;
            _logger = logger;
        }

        public Task Consume(ConsumeContext<WorldOnlineMessage> context)
        {
            var message = context.Message;
            var update = _worldSessions.ObserveOnline(new WorldSessionContext(
                message.Id,
                message.Name,
                message.InstanceId,
                message.Generation,
                message.LeaseExpiresAt));

            if (update.IsAvailable)
            {
                _logger.LogInformation("Registered world generation: {Id} - {Name}", message.Id, message.Name);
            }
            else if (update.Changed)
            {
                _logger.LogWarning("World {Id} has multiple live generations; contact routing is paused.", message.Id);
            }

            return Task.CompletedTask;
        }

        public async Task Consume(ConsumeContext<WorldOfflineMessage> context)
        {
            var message = context.Message;
            var update = _worldSessions.ObserveOffline(message.Id, message.InstanceId, message.Generation);
            if (!update.Changed)
            {
                _logger.LogDebug("Ignoring stale offline status for world {Id} from instance {InstanceId}", message.Id, message.InstanceId);
                return;
            }

            if (update.IsAvailable)
            {
                _logger.LogInformation("Retained surviving world generation: {Id} - {InstanceId}", message.Id, update.ActiveSession!.InstanceId);
                return;
            }

            await _contactSessionService.RemoveWorldSessions(message.Id);
            _logger.LogInformation("Removed world: {Id}", message.Id);
        }
    }
}
