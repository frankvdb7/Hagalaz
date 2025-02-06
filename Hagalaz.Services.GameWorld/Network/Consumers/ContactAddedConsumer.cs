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
    public class ContactAddedConsumer : IConsumer<ContactAddedMessage>
    {
        private readonly IGameConnectionService _connectionService;
        private readonly IMapper _mapper;

        public ContactAddedConsumer(IGameConnectionService connectionService, IMapper mapper)
        {
            _connectionService = connectionService;
            _mapper = mapper;
        }

        public async Task Consume(ConsumeContext<ContactAddedMessage> context)
        {
            var message = context.Message;
            var characterContact = await _connectionService.FindByMasterId(message.Contact.MasterId);
            var masterFriend = characterContact?.Features?.Get<IContactsFeature>()?.Friends?.Get(message.Master.MasterId);
            if (characterContact == null || masterFriend == null)
            {
                return;
            }
            masterFriend.AreMutualFriends = message.Master.AreMutualFriends;
            var masterContact = _mapper.Map<ContactDto>(message.Master);
            await characterContact.SendMessage(new FriendsListMessage()
            {
                Friends = new List<ContactDto>()
                {
                    masterContact
                },
                Notify = true
            });
        }
    }
}