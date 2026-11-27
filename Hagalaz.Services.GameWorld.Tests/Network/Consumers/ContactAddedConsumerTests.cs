using AutoMapper;
using Hagalaz.Contacts.Messages;
using Hagalaz.Game.Abstractions.Features.Chat;
using Hagalaz.Game.Abstractions.Features.FriendsChat;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model;
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
    [DataRow(Availability.Nobody, true, false)]
    [DataRow(Availability.Friends, false, false)]
    [DataRow(Availability.Friends, true, true)]
    [DataRow(Availability.Everyone, false, true)]
    public async Task ReverseAdd_Revision742PacketRevealsWorldOnlyWhenEligible(Availability availability, bool publisherMutual, bool expectVisible)
    {
        var (connectionService, connection) = CreateConnection(CreateFriend(availability));
        FriendsListMessage? sent = null;
        await connection.SendMessage(Arg.Do<RaidoMessage>(m => sent = m as FriendsListMessage));

        await new ContactAddedConsumer(connectionService, CreateMapper())
            .Consume(CreateContext(CreateMessage(publisherMutual)));

        Assert.IsNotNull(sent);
        var packet = Encode(sent);
        var worldName = System.Text.Encoding.ASCII.GetBytes("World 3");
        Assert.AreEqual(expectVisible, packet.AsSpan().IndexOf(worldName) >= 0);
        if (!expectVisible)
        {
            var hidden = Encode(new FriendsListMessage
            {
                Notify = true,
                Friends = [new ContactDto { MasterId = (int)AdderId, DisplayName = "Adder" }]
            });
            CollectionAssert.AreEqual(hidden, packet);
        }
    }

    private static byte[] Encode(FriendsListMessage message)
    {
        using var buffer = Raido.Common.Buffers.MemoryBufferWriter.Get();
        new Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders.FriendsListMessageEncoder()
            .EncodeMessage(message, new RaidoMessageBinaryWriter(buffer));
        return buffer.ToArray();
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
        var offline = CreateMessage(publisherMutual: true);
        offline = offline with
        {
            Master = offline.Master with { WorldId = null, WorldName = null, SessionGeneration = null, SessionConnectionId = null }
        };
        await consumer.Consume(CreateContext(offline));

        await connection.Received(1).SendMessage(Arg.Is<FriendsListMessage>(m =>
            m.Friends[0].WorldId == null && m.Friends[0].WorldName == null));
    }

    [TestMethod]
    public async Task ReverseAdd_AfterInitialSnapshotWithoutSessionIdentity_ShowsPublishedSession()
    {
        var friend = CreateFriend(Availability.Everyone);
        var feature = CreateFeature(friend, signedIn: false);
        ApplyInitialSnapshot(feature, friend);
        Assert.IsFalse(feature.GetPresence(AdderId).IsOnline);
        var (connectionService, connection) = CreateConnection(feature);

        await new ContactAddedConsumer(connectionService, CreateMapper())
            .Consume(CreateContext(CreateMessage(publisherMutual: true)));

        await connection.Received(1).SendMessage(Arg.Is<FriendsListMessage>(m =>
            m.Friends[0].WorldId == 3 && m.Friends[0].WorldName == "World 3"));
        Assert.IsTrue(feature.GetPresence(AdderId).IsOnline);
    }

    [TestMethod]
    public async Task ReverseAdd_AfterInitialSnapshotWithFriendsOnlyNonMutual_BecomesVisibleOnceMutual()
    {
        var friend = CreateFriend(Availability.Friends);
        var feature = CreateFeature(friend, signedIn: false);
        ApplyInitialSnapshot(feature, friend);
        var (connectionService, connection) = CreateConnection(feature);

        await new ContactAddedConsumer(connectionService, CreateMapper())
            .Consume(CreateContext(CreateMessage(publisherMutual: true)));

        await connection.Received(1).SendMessage(Arg.Is<FriendsListMessage>(m =>
            m.Friends[0].WorldId == 3 && m.Friends[0].WorldName == "World 3"));
    }

    [TestMethod]
    public async Task ReverseAdd_WithStaleSessionDoesNotReviveSignedOutOwner()
    {
        var friend = CreateFriend(Availability.Everyone);
        var feature = CreateFeature(friend, signedIn: false);
        feature.TryApplySignIn(AdderId, 2, "newer", 4, "World 4");
        feature.TryApplySignOut(AdderId, 2, "newer");
        var (connectionService, connection) = CreateConnection(feature);

        await new ContactAddedConsumer(connectionService, CreateMapper())
            .Consume(CreateContext(CreateMessage(publisherMutual: true)));

        await connection.Received(1).SendMessage(Arg.Is<FriendsListMessage>(m =>
            m.Friends[0].WorldId == null && m.Friends[0].WorldName == null));
        Assert.IsFalse(feature.GetPresence(AdderId).IsOnline);
    }

    private static Friend CreateFriend(Availability availability) => new()
    {
        MasterId = (int)AdderId,
        Rank = FriendsChatRank.Friend,
        Availability = availability,
        AreMutualFriends = false
    };

    private static ContactsFeature CreateFeature(Friend friend, bool signedIn)
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

        return feature;
    }

    // Mirrors ContactService.MapContactsAsync omitting the session identity of a non-mutual friend.
    private static void ApplyInitialSnapshot(ContactsFeature feature, Friend friend)
    {
        var snapshotContact = new Hagalaz.Contacts.Messages.Model.ContactDto { MasterId = AdderId, DisplayName = "Adder" };
        var response = new GetContactsResponse
        {
            MasterId = ReceiverId,
            SessionGeneration = 1,
            ConnectionId = "receiver",
            Friends = [snapshotContact],
            Ignores = []
        };
        var session = Substitute.For<IGameSession>();
        session.MasterId.Returns(ReceiverId);
        session.SessionGeneration.Returns(1);
        session.ConnectionId.Returns("receiver");
        var mapper = Substitute.For<IMapper>();
        mapper.Map<IEnumerable<Friend>>(Arg.Any<object>()).Returns(new[] { friend });
        mapper.Map<IEnumerable<Ignore>>(Arg.Any<object>()).Returns(Array.Empty<Ignore>());
        mapper.Map<List<ContactDto>>(Arg.Any<object>())
            .Returns(call => new List<ContactDto>());
        mapper.Map<List<ContactDto>>(response.Friends)
            .Returns(new List<ContactDto> { new() { MasterId = (int)AdderId, DisplayName = "Adder" } });

        Assert.IsTrue(ContactSnapshotApplicator.TryApply(response, session, feature, mapper));
    }

    private static (IGameConnectionService Service, IGameConnection Connection) CreateConnection(Friend friend, bool signedIn = true) =>
        CreateConnection(CreateFeature(friend, signedIn));

    private static (IGameConnectionService Service, IGameConnection Connection) CreateConnection(ContactsFeature feature)
    {
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
            SessionGeneration = 1,
            SessionConnectionId = "conn",
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
