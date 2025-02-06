using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Contacts.Messages;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Game.Messages.Protocol.Model;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Services;
using MassTransit;
using Hagalaz.Game.Extensions;

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
            await foreach (var connection in _connectionService.FindAll().Where(c => c.Features.Get<IContactsFeature>()?.Friends?.Contains(message.Contact.MasterId) ?? false))
            {
                var friend = connection.Features.Get<IContactsFeature>()?.Friends?.Get(message.Contact.MasterId);
                if (friend == null)
                {
                    continue;
                }
                var friendUpdateMessage = new FriendsListMessage
                {
                    Friends = new List<ContactDto>
                    {
                        friendContact with
                        {
                            WorldId = friend.GetWorldId(friendContact.WorldId)
                        }
                    },
                    Notify = true
                };
                await connection.SendMessage(friendUpdateMessage);
            }
        }
    }
}
