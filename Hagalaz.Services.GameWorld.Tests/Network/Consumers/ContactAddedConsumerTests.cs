using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Contacts.Messages;
using Hagalaz.Game.Abstractions.Features.Chat;
using Hagalaz.Game.Abstractions.Features.FriendsChat;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Game.Messages.Protocol.Model;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Network.Consumers;
using Hagalaz.Services.GameWorld.Network.Model;
using Hagalaz.Services.GameWorld.Services;
using MassTransit;
using Microsoft.AspNetCore.Http.Features;
using NSubstitute;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Tests.Network.Consumers;

[TestClass]
public sealed class ContactAddedConsumerTests
{
    private const uint ReceiverId = 7;
    private const uint AdderId = 42;

    [TestMethod]
    [DataRow(Availability.Nobody, false, true)]
    [DataRow(Availability.Friends, false, true)]
    [DataRow(Availability.Friends, true, false)]
    [DataRow(Availability.Everyone, false, false)]
    public async Task ReverseAdd_AppliesPrivacyGateToOutgoingWorld(Availability availability, bool publisherMutual, bool expectHidden)
    {
        var friend = new Friend
        {
            MasterId = (int)AdderId,
            Rank = FriendsChatRank.Friend,
            Availability = availability,
            AreMutualFriends = false
        };
        var (connectionService, connection) = CreateConnection(friend);

        var consumer = new ContactAddedConsumer(connectionService, CreateMapper());
        await consumer.Consume(CreateContext(CreateMessage(publisherMutual)));

        await connection.Received(1).SendMessage(Arg.Is<FriendsListMessage>(m =>
            m.Notify &&
            m.Friends.Count == 1 &&
            (expectHidden
                ? m.Friends[0].WorldId == null && m.Friends[0].WorldName == null
                : m.Friends[0].WorldId == 3 && m.Friends[0].WorldName == "World 3")));
    }

    [TestMethod]
    public async Task ReverseAdd_UsesPublishedMutualStatusForReceiversFriendRecord()
    {
        var friend = new Friend
        {
            MasterId = (int)AdderId,
            Rank = FriendsChatRank.Friend,
            Availability = Availability.Friends,
            AreMutualFriends = false
        };
        var (connectionService, _) = CreateConnection(friend);

        var consumer = new ContactAddedConsumer(connectionService, CreateMapper());
        await consumer.Consume(CreateContext(CreateMessage(publisherMutual: true)));

        Assert.IsTrue(friend.AreMutualFriends);
    }

    [TestMethod]
    public async Task ReverseAdd_OfflineAdderReportsNoWorld()
    {
        var friend = new Friend
        {
            MasterId = (int)AdderId,
            Rank = FriendsChatRank.Friend,
            Availability = Availability.Everyone,
            AreMutualFriends = false
        };
        var (connectionService, connection) = CreateConnection(friend, signedIn: false);

        var consumer = new ContactAddedConsumer(connectionService, CreateMapper());
        await consumer.Consume(CreateContext(CreateMessage(publisherMutual: true)));

        await connection.Received(1).SendMessage(Arg.Is<FriendsListMessage>(m =>
            m.Friends[0].WorldId == null && m.Friends[0].WorldName == null));
    }

    private static (IGameConnectionService Service, IGameConnection Connection) CreateConnection(Friend friend, bool signedIn = true)
    {
        var contacts = new ContactList<Friend>();
        contacts.Add(friend);
        var character = Substitute.For<ICharacter>();
        character.Friends.Returns(contacts);
        character.Ignores.Returns(new ContactList<Ignore>());
        var feature = new ContactsFeature(character);
        if (signedIn)
        {
            feature.TryApplySignIn(AdderId, 1, "conn", 3, "World 3");
        }

        var features = new FeatureCollection();
        features.Set<IContactsFeature>(feature);
        var connection = Substitute.For<IGameConnection>();
        connection.Features.Returns(features);
        connection.SendMessage(Arg.Any<RaidoMessage>()).Returns(Task.CompletedTask);

        var service = Substitute.For<IGameConnectionService>();
        service.FindByMasterId(ReceiverId).Returns(connection);
        return (service, connection);
    }

    private static ContactAddedMessage CreateMessage(bool publisherMutual) => new(
        new Hagalaz.Contacts.Messages.Model.ContactDto
        {
            MasterId = AdderId,
            DisplayName = "Adder",
            WorldId = 3,
            WorldName = "World 3",
            AreMutualFriends = publisherMutual
        },
        new Hagalaz.Contacts.Messages.Model.ContactDto { MasterId = ReceiverId, DisplayName = "Receiver" });

    private static IMapper CreateMapper()
    {
        var mapper = Substitute.For<IMapper>();
        mapper.Map<ContactDto>(Arg.Any<Hagalaz.Contacts.Messages.Model.ContactDto>()).Returns(call =>
        {
            var source = call.Arg<Hagalaz.Contacts.Messages.Model.ContactDto>();
            return new ContactDto
            {
                MasterId = (int)source.MasterId,
                DisplayName = source.DisplayName,
                WorldId = source.WorldId,
                WorldName = source.WorldName
            };
        });
        return mapper;
    }

    private static ConsumeContext<T> CreateContext<T>(T message) where T : class
    {
        var context = Substitute.For<ConsumeContext<T>>();
        context.Message.Returns(message);
        return context;
    }
}
