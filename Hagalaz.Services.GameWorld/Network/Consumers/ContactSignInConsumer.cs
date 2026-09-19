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
    public class ContactSignInConsumer : IConsumer<ContactSignInMessage>
    {
        private readonly IGameConnectionService _connectionService;
        private readonly IMapper _mapper;

        public ContactSignInConsumer(IGameConnectionService connectionService, IMapper mapper)
        {
            _connectionService = connectionService;
            _mapper = mapper;
        }

        public async Task Consume(ConsumeContext<ContactSignInMessage> context)
        {
            var message = context.Message;
            var friendContact = _mapper.Map<ContactDto>(message.Contact);
            await foreach (var connection in _connectionService.FindAll())
            {
                var contacts = connection.Features.Get<IContactsFeature>();
                var friend = contacts?.TryApplySignIn(
                    message.Contact.MasterId,
                    message.SessionGeneration,
                    message.ConnectionId,
                    message.Contact.WorldId,
                    message.Contact.WorldName);
                if (friend == null)
                {
                    continue;
                }
                var friendUpdateMessage = new FriendsListMessage
                {
                    Friends = new List<ContactDto>
                    {
                        ContactSnapshotApplicator.ReconcileFriendContact(
                            friend,
                            friendContact,
                            contacts!.GetPresence(message.Contact.MasterId))
                    },
                    Notify = true
                };
                await connection.SendMessage(friendUpdateMessage);
            }
        }
    }
}
