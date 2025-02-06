using Hagalaz.Game.Messages;
using Hagalaz.Services.Contacts.Services;
using MassTransit;

namespace Hagalaz.Services.Contacts.Consumers
{
    public class LobbyUserSignInOutConsumer : IConsumer<LobbyUserSignInMessage>, IConsumer<LobbyUserSignOutMessage>
    {
        private readonly IContactSessionService _contactSessionService;

        public LobbyUserSignInOutConsumer(IContactSessionService contactSessionService)
        {
            _contactSessionService = contactSessionService;
        }

        public async Task Consume(ConsumeContext<LobbyUserSignInMessage> context)
        {
            var message = context.Message;
            await _contactSessionService.AddLobbySession(message.WorldId, message.MasterId);
        }

        public async Task Consume(ConsumeContext<LobbyUserSignOutMessage> context)
        {
            var message = context.Message;
            await _contactSessionService.RemoveSession(message.MasterId);
        }
    }
}
