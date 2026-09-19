using System.Collections.Generic;
using System.Linq;
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
            var connection = await _connectionService.FindById(message.ConnectionId);
            if (connection == null)
            {
                return;
            }

            var session = connection.Features.Get<ISessionFeature>()?.Session;
            if (connection.ConnectionId != message.ConnectionId ||
                session is null ||
                session.MasterId != message.MasterId ||
                session.SessionGeneration != message.SessionGeneration ||
                session.ConnectionId != message.ConnectionId)
            {
                return;
            }

            var friendsList = _mapper.Map<IEnumerable<Friend>>(message.Friends);
            var ignoreList = _mapper.Map<IEnumerable<Ignore>>(message.Ignores);
            var friendContacts = _mapper.Map<List<ContactDto>>(message.Friends);
            var ignoreContacts = _mapper.Map<List<ContactDto>>(message.Ignores);

            var contactFeature = connection.Features.Get<IContactsFeature>();
            if (contactFeature is null)
            {
                return;
            }

            try
            {
                contactFeature.ReplaceFriends(
                    friendsList,
                    message.Friends
                        .Where(contact => contact.WorldId is not null &&
                                          contact.SessionGeneration is not null &&
                                          contact.SessionConnectionId is not null)
                        .Select(contact => new ContactPresenceOwner(
                            contact.MasterId,
                            contact.SessionGeneration!.Value,
                            contact.SessionConnectionId!)),
                    message.ObservationBoundary);
                contactFeature.Ignores.Set(ignoreList);

                await Task.WhenAll(
                    connection.SendMessage(new FriendsListMessage { Friends = friendContacts }, context.CancellationToken),
                    connection.SendMessage(new IgnoreListMessage { Ignores = ignoreContacts }, context.CancellationToken)
                );
            }
            finally
            {
                contactFeature.EndObservationWindow();
            }
        }
    }
}
