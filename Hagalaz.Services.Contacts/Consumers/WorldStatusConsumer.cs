using Hagalaz.Contacts.Messages;
using Hagalaz.Contacts.Messages.Model;
using Hagalaz.Game.Messages;
using Hagalaz.Services.Contacts.Services;
using Hagalaz.Services.Contacts.Store;
using Hagalaz.Services.Contacts.Store.Model;
using MassTransit;

namespace Hagalaz.Services.Contacts.Consumers
{
    public class WorldStatusConsumer : IConsumer<WorldOnlineMessage>, IConsumer<WorldOfflineMessage>
    {
        private readonly ICharacterService _characterService;
        private readonly WorldSessionStore _worldSessions;
        private readonly ContactSessionStore _contactSessions;
        private readonly ILogger<WorldStatusConsumer> _logger;

        public WorldStatusConsumer(
            ICharacterService characterService,
            WorldSessionStore worldSessions,
            ContactSessionStore contactSessions,
            ILogger<WorldStatusConsumer> logger)
        {
            _characterService = characterService;
            _worldSessions = worldSessions;
            _contactSessions = contactSessions;
            _logger = logger;
        }

        public Task Consume(ConsumeContext<WorldOnlineMessage> context)
        {
            var message = context.Message;
            var session = new WorldSessionContext(
                message.Id,
                message.Name,
                message.InstanceId,
                message.Generation,
                message.LeaseExpiresAt);

            if (_worldSessions.TryGetValue(message.Id, out var existing))
            {
                if (!ShouldReplace(existing, session))
                {
                    return Task.CompletedTask;
                }

                _worldSessions[message.Id] = session;
                _logger.LogInformation("Replaced world generation: {Id} - {Name}", message.Id, message.Name);
                return Task.CompletedTask;
            }

            if (_worldSessions.TryAdd(message.Id, session))
            {
                _logger.LogInformation("Registered world: {Id} - {Name}", message.Id, message.Name);
            }

            return Task.CompletedTask;
        }

        public async Task Consume(ConsumeContext<WorldOfflineMessage> context)
        {
            var message = context.Message;
            if (!_worldSessions.TryGetValue(message.Id, out var current) ||
                current.InstanceId != message.InstanceId ||
                current.Generation != message.Generation)
            {
                _logger.LogDebug("Ignoring stale offline status for world {Id} from instance {InstanceId}", message.Id, message.InstanceId);
                return;
            }

            try
            {
                var offlineMessages = await _contactSessions.ToAsyncEnumerable()
                    .Select(new Func<ContactSessionContext, CancellationToken, ValueTask<ContactSignOutMessage?>>(async (session, ct) =>
                    {
                        var contact = await _characterService.FindCharacterByIdAsync(session.MasterId);
                        if (contact == null)
                        {
                            return null;
                        }

                        var dto = new ContactDto
                        {
                            MasterId = contact.MasterId, DisplayName = contact.DisplayName, PreviousDisplayName = contact.PreviousDisplayName
                        };
                        return new ContactSignOutMessage(dto);
                    }))
                    .OfType<ContactSignOutMessage>()
                    .ToListAsync();
                await context.PublishBatch(offlineMessages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish offline contacts");
            }
            finally
            {
                if (_worldSessions.TryGetValue(message.Id, out current) &&
                    current.InstanceId == message.InstanceId && current.Generation == message.Generation &&
                    _worldSessions.TryRemove(message.Id))
                {
                    _logger.LogInformation("Removed world: {Id}", message.Id);
                }
            }
        }

        private static bool ShouldReplace(WorldSessionContext current, WorldSessionContext candidate)
        {
            if (current.InstanceId == candidate.InstanceId)
            {
                return candidate.Generation > current.Generation || candidate.LeaseExpiresAt > current.LeaseExpiresAt;
            }

            return candidate.Generation > current.Generation;
        }
    }
}
