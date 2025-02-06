using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Contacts.Messages;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Game.Messages.Protocol.Model;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Services;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Network.Consumers
{
    public class GetContactsResponseConsumer : IConsumer<GetContactsResponse>
    {
        private readonly IGameConnectionService _connectionService;
        private readonly IMapper _mapper;

        public GetContactsResponseConsumer(IGameConnectionService connectionService, IMapper mapper)
        {
            _connectionService = connectionService;
            _mapper = mapper;
        }

        public async Task Consume(ConsumeContext<GetContactsResponse> context)
        {
            var message = context.Message;
            var connection = await _connectionService.FindByMasterId(message.MasterId);
            if (connection == null)
            {
                return;
            }
            var friendsList = _mapper.Map<IEnumerable<Friend>>(message.Friends);
            var ignoreList = _mapper.Map<IEnumerable<Ignore>>(message.Ignores);
            var friendContacts = _mapper.Map<List<ContactDto>>(message.Friends);
            var ignoreContacts = _mapper.Map<List<ContactDto>>(message.Ignores);

            var contactFeature = connection.Features.Get<IContactsFeature>();
            contactFeature?.Friends?.Set(friendsList);
            contactFeature?.Ignores?.Set(ignoreList);

            await Task.WhenAll(
                connection.SendMessage(new FriendsListMessage { Friends = friendContacts }, context.CancellationToken),
                connection.SendMessage(new IgnoreListMessage { Ignores = ignoreContacts }, context.CancellationToken)
            );
        }
    }
}
