using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Contacts.Messages;
using Hagalaz.Game.Abstractions.Model;
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
using Microsoft.AspNetCore.Http.Features;
using MassTransit;
using NSubstitute;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Tests.Network.Consumers;

[TestClass]
public sealed class GetContactsResponseConsumerTests
{
    [TestMethod]
    public async Task Consume_DropsResponseForDifferentSessionOnTheSameAccount()
    {
        var feature = new LobbyContactsFeature();
        feature.ReplaceFriends([CreateFriend(7)], []);
        var session = Substitute.For<IGameSession>();
        session.MasterId.Returns(42u);
        session.SessionGeneration.Returns(2L);
        session.ConnectionId.Returns("current");
        var connection = CreateConnection("current", session, feature);
        var connections = Substitute.For<IGameConnectionService>();
        connections.FindById("old").Returns(connection);
        var consumer = new GetContactsResponseConsumer(connections, Substitute.For<IMapper>());

        await consumer.Consume(CreateContext(new GetContactsResponse
        {
            MasterId = 42,
            SessionGeneration = 1,
            ConnectionId = "old",
            Friends = new[] { new Hagalaz.Contacts.Messages.Model.ContactDto { MasterId = 99, DisplayName = "Friend" } },
            Ignores = [],
            ObservationBoundary = 0
        }));

        Assert.IsNotNull(feature.Friends.Get(7));
        Assert.IsNull(feature.Friends.Get(99));
        await connection.DidNotReceive().SendMessage(Arg.Any<RaidoMessage>());
    }

    [TestMethod]
    public async Task Consume_AcceptsOnlyTheExactCurrentSession()
    {
        var feature = new LobbyContactsFeature();
        var session = Substitute.For<IGameSession>();
        session.MasterId.Returns(42u);
        session.SessionGeneration.Returns(2L);
        session.ConnectionId.Returns("current");
        var connection = CreateConnection("current", session, feature);
        var connections = Substitute.For<IGameConnectionService>();
        connections.FindById("current").Returns(connection);
        var mapper = Substitute.For<IMapper>();
        mapper.Map<IEnumerable<Friend>>(Arg.Any<IEnumerable<Hagalaz.Contacts.Messages.Model.ContactDto>>()).Returns(new[] { CreateFriend(99) });
        mapper.Map<IEnumerable<Ignore>>(Arg.Any<IEnumerable<Hagalaz.Contacts.Messages.Model.ContactDto>>()).Returns(Array.Empty<Ignore>());
        mapper.Map<List<ContactDto>>(Arg.Any<IEnumerable<Hagalaz.Contacts.Messages.Model.ContactDto>>()).Returns(new List<ContactDto>());
        var consumer = new GetContactsResponseConsumer(connections, mapper);

        await consumer.Consume(CreateContext(new GetContactsResponse
        {
            MasterId = 42,
            SessionGeneration = 2,
            ConnectionId = "current",
            Friends = new[] { new Hagalaz.Contacts.Messages.Model.ContactDto { MasterId = 99, DisplayName = "Friend" } },
            Ignores = [],
            ObservationBoundary = 0
        }));

        Assert.IsNotNull(feature.Friends.Get(99));
        await connection.Received(2).SendMessage(Arg.Any<RaidoMessage>(), Arg.Any<CancellationToken>());
    }

    private static IGameConnection CreateConnection(string connectionId, IGameSession session, IContactsFeature contacts)
    {
        var connection = Substitute.For<IGameConnection>();
        connection.ConnectionId.Returns(connectionId);
        var features = new FeatureCollection();
        features.Set<Hagalaz.Services.GameWorld.Features.ISessionFeature>(new SessionFeature { Session = session });
        features.Set(contacts);
        connection.Features.Returns(features);
        connection.SendMessage(Arg.Any<RaidoMessage>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        return connection;
    }

    private static Friend CreateFriend(uint masterId) => new()
    {
        MasterId = checked((int)masterId),
        Rank = FriendsChatRank.Friend,
        Availability = Availability.Everyone,
        AreMutualFriends = true
    };

    private static ConsumeContext<GetContactsResponse> CreateContext(GetContactsResponse response)
    {
        var context = Substitute.For<ConsumeContext<GetContactsResponse>>();
        context.Message.Returns(response);
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }
}
