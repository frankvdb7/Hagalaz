using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Contacts.Messages;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Game.Messages.Protocol.Model;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Services;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Network.Consumers
{
    public class ContactSignOutConsumer : IConsumer<ContactSignOutMessage>
    {
        private readonly IGameConnectionService _connectionService;
        private readonly IMapper _mapper;

        public ContactSignOutConsumer(IGameConnectionService connectionService, IMapper mapper)
        {
            _connectionService = connectionService;
            _mapper = mapper;
        }

        public async Task Consume(ConsumeContext<ContactSignOutMessage> context)
        {
            var message = context.Message;
            await foreach (var connection in _connectionService.FindAll())
            {
                var contacts = connection.Features.Get<IContactsFeature>();
                var currentFriend = contacts?.TryApplySignOut(
                    message.Contact.MasterId,
                    message.SessionGeneration,
                    message.ConnectionId);
                if (currentFriend is null)
                {
                    continue;
                }
                var friend = _mapper.Map<ContactDto>(message.Contact) with
                {
                    WorldId = null,
                    WorldName = null
                };
                var friendUpdateMessage = new FriendsListMessage { Friends = new List<ContactDto> { friend }, Notify = true };
                await connection.SendMessage(friendUpdateMessage);
            }
        }
    }
}
