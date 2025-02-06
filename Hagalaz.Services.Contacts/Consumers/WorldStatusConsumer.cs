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

        public WorldStatusConsumer(ICharacterService characterService, WorldSessionStore worldSessions, ContactSessionStore contactSessions, ILogger<WorldStatusConsumer> logger)
        {
            _characterService = characterService;
            _worldSessions = worldSessions;
            _contactSessions = contactSessions;
            _logger = logger;
        }

        public Task Consume(ConsumeContext<WorldOnlineMessage> context)
        {
            var message = context.Message;
            if (_worldSessions.ContainsKey(message.Id))
            {
                return Task.CompletedTask;
            }
            if (_worldSessions.TryAdd(message.Id, new WorldSessionContext(message.Id, message.Name)))
            {
                _logger.LogInformation("Registered world: {Id} - {Name}", message.Id, message.Name);
            }
            return Task.CompletedTask;
        }

        public async Task Consume(ConsumeContext<WorldOfflineMessage> context)
        {
            var message = context.Message;
            try
            {
                var offlineMessages = await _contactSessions.ToAsyncEnumerable()
                    .SelectAwait(async session =>
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
                    })
                    .OfType<ContactSignOutMessage>()
                    .ToListAsync();
                await context.PublishBatch(offlineMessages);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Failed to publish offline contacts");
            }
            finally
            {
                if (_worldSessions.TryRemove(message.Id))
                {
                    _logger.LogInformation("Removed world: {Id}", message.Id);
                }
            }
        }
    }
}
