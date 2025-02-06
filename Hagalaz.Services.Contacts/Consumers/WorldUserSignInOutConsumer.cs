using Hagalaz.Game.Messages;
using Hagalaz.Services.Contacts.Services;
using MassTransit;

namespace Hagalaz.Services.Contacts.Consumers
{
    public class WorldUserSignInOutConsumer : IConsumer<WorldUserSignInMessage>, IConsumer<WorldUserSignOutMessage>
    {
        private readonly IContactSessionService _contactSessionService;

        public WorldUserSignInOutConsumer(IContactSessionService contactSessionService) => _contactSessionService = contactSessionService;

        public async Task Consume(ConsumeContext<WorldUserSignInMessage> context)
        {
            var message = context.Message;
            await _contactSessionService.AddWorldSession(message.WorldId, message.MasterId);
        }

        public async Task Consume(ConsumeContext<WorldUserSignOutMessage> context)
        {
            var message = context.Message;
            await _contactSessionService.RemoveSession(message.MasterId);
        }
    }
}
