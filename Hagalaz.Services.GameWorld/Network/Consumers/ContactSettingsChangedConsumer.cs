using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Contacts.Messages;
using Hagalaz.Game.Abstractions.Features.Chat;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Game.Messages.Protocol.Model;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Services;
using MassTransit;
using Hagalaz.Game.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Consumers
{
    public class ContactSettingsChangedConsumer : IConsumer<ContactSettingsChangedMessage>
    {
        private readonly IGameConnectionService _connectionService;
        private readonly IMapper _mapper;

        public ContactSettingsChangedConsumer(IGameConnectionService connectionService, IMapper mapper)
        {
            _connectionService = connectionService;
            _mapper = mapper;
        }

        public async Task Consume(ConsumeContext<ContactSettingsChangedMessage> context)
        {
            var message = context.Message;
            if (message.MasterContact.Settings == null)
            {
                return;
            }
            var contactDto = _mapper.Map<ContactDto>(message.MasterContact);
            await foreach (var connection in _connectionService.FindAll().Where(c => c.Features.Get<IContactsFeature>()?.Friends?.Contains(message.MasterId) ?? false))
            {
                var friend = connection.Features.Get<IContactsFeature>()?.Friends?.Get(message.MasterId);
                if (friend == null)
                {
                    continue;
                }
                friend.Availability = (Availability)message.MasterContact.Settings.Availability;
                var friendMessage = new FriendsListMessage
                {
                    Friends = new List<ContactDto>
                    {
                        contactDto with
                        {
                            WorldId = friend.GetWorldId(contactDto.WorldId)
                        }
                    },
                    Notify = true
                };
                await connection.SendMessage(friendMessage);
            }
        }
    }
}