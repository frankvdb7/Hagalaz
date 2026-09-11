using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
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
using Microsoft.AspNetCore.Http.Features;
using MassTransit;
using NSubstitute;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Tests.Network.Consumers;

[TestClass]
public sealed class ContactPresenceConsumerTests
{
    [TestMethod]
    public async Task SignInThenLateOlderSignOut_LeavesTheNewerPresenceOnline()
    {
        var contacts = new ContactList<Friend>();
        contacts.Add(new Friend
        {
            MasterId = 42,
            Rank = FriendsChatRank.Friend,
            Availability = Availability.Everyone,
            AreMutualFriends = true
        });
        var connection = CreateConnection(contacts);
        var connectionService = Substitute.For<IGameConnectionService>();
        connectionService.FindAll().Returns(One(connection));
        var mapper = CreateMapper();
        var signInConsumer = new ContactSignInConsumer(connectionService, mapper);
        var signOutConsumer = new ContactSignOutConsumer(connectionService, mapper);

        await signInConsumer.Consume(CreateContext(new ContactSignInMessage(CreateContact(), 10, "old")));
        await signInConsumer.Consume(CreateContext(new ContactSignInMessage(CreateContact(), 11, "new")));
        connection.ClearReceivedCalls();
        await signOutConsumer.Consume(CreateContext(new ContactSignOutMessage(CreateContact(), 10, "old")));

        await connection.DidNotReceive().SendMessage(Arg.Is<FriendsListMessage>(message =>
            message.Friends.Count == 1 && message.Friends[0].WorldId == null));
    }

    [TestMethod]
    public async Task SignOutWithWrongConnectionDoesNotRemoveCurrentPresence()
    {
        var contacts = new ContactList<Friend>();
        contacts.Add(new Friend
        {
            MasterId = 42,
            Rank = FriendsChatRank.Friend,
            Availability = Availability.Everyone,
            AreMutualFriends = true
        });
        var connection = CreateConnection(contacts);
        var connectionService = Substitute.For<IGameConnectionService>();
        connectionService.FindAll().Returns(One(connection));
        var signInConsumer = new ContactSignInConsumer(connectionService, CreateMapper());
        await signInConsumer.Consume(CreateContext(new ContactSignInMessage(CreateContact(), 10, "current")));
        connection.ClearReceivedCalls();
        var consumer = new ContactSignOutConsumer(connectionService, CreateMapper());

        await consumer.Consume(CreateContext(new ContactSignOutMessage(CreateContact(), 10, "other")));

        await connection.DidNotReceive().SendMessage(Arg.Any<RaidoMessage>());
    }

    [TestMethod]
    public async Task SignOutWithCurrentGenerationAndConnectionClearsPresence()
    {
        var contacts = new ContactList<Friend>();
        contacts.Add(new Friend
        {
            MasterId = 42,
            Rank = FriendsChatRank.Friend,
            Availability = Availability.Everyone,
            AreMutualFriends = true
        });
        var connection = CreateConnection(contacts);
        var connectionService = Substitute.For<IGameConnectionService>();
        connectionService.FindAll().Returns(One(connection));
        var signInConsumer = new ContactSignInConsumer(connectionService, CreateMapper());
        await signInConsumer.Consume(CreateContext(new ContactSignInMessage(CreateContact(), 10, "current")));
        connection.ClearReceivedCalls();
        var consumer = new ContactSignOutConsumer(connectionService, CreateMapper());

        await consumer.Consume(CreateContext(new ContactSignOutMessage(CreateContact(), 10, "current")));

        await connection.Received(1).SendMessage(Arg.Any<FriendsListMessage>());
    }

    [TestMethod]
    public async Task OldSignInCannotOverwriteNewerSignIn()
    {
        var contacts = new ContactList<Friend>();
        contacts.Add(new Friend
        {
            MasterId = 42,
            Rank = FriendsChatRank.Friend,
            Availability = Availability.Everyone,
            AreMutualFriends = true
        });
        var connection = CreateConnection(contacts);
        var connectionService = Substitute.For<IGameConnectionService>();
        connectionService.FindAll().Returns(One(connection));
        var consumer = new ContactSignInConsumer(connectionService, CreateMapper());

        await consumer.Consume(CreateContext(new ContactSignInMessage(CreateContact(), 2, "new")));
        await consumer.Consume(CreateContext(new ContactSignInMessage(CreateContact(), 1, "old")));

        connection.ClearReceivedCalls();
        var signOutConsumer = new ContactSignOutConsumer(connectionService, CreateMapper());
        await signOutConsumer.Consume(CreateContext(new ContactSignOutMessage(CreateContact(), 1, "old")));

        await connection.DidNotReceive().SendMessage(Arg.Is<FriendsListMessage>(message =>
            message.Friends.Count == 1 && message.Friends[0].WorldId == null));
    }

    [TestMethod]
    public async Task DuplicateSignInForCurrentOwnerDoesNotNotifyAgain()
    {
        var contacts = new ContactList<Friend>();
        contacts.Add(new Friend
        {
            MasterId = 42,
            Rank = FriendsChatRank.Friend,
            Availability = Availability.Everyone,
            AreMutualFriends = true
        });
        var connection = CreateConnection(contacts);
        var connectionService = Substitute.For<IGameConnectionService>();
        connectionService.FindAll().Returns(One(connection));
        var consumer = new ContactSignInConsumer(connectionService, CreateMapper());
        var message = new ContactSignInMessage(CreateContact(), 2, "current");

        await consumer.Consume(CreateContext(message));
        connection.ClearReceivedCalls();
        await consumer.Consume(CreateContext(message));

        await connection.DidNotReceive().SendMessage(Arg.Any<RaidoMessage>());
    }

    [TestMethod]
    public async Task ConcurrentNewSignInAndOlderSignOutLeaveTheNewOwnerOnline()
    {
        var contacts = new ContactList<Friend>();
        contacts.Add(new Friend
        {
            MasterId = 42,
            Rank = FriendsChatRank.Friend,
            Availability = Availability.Everyone,
            AreMutualFriends = true
        });
        var connection = CreateConnection(contacts);
        var feature = connection.Features.Get<IContactsFeature>()!;
        Assert.IsNotNull(feature.TryApplySignIn(42, 10, "old"));

        await Task.WhenAll(
            Task.Run(() => feature.TryApplySignIn(42, 11, "new")),
            Task.Run(() => feature.TryApplySignOut(42, 10, "old")));

        Assert.IsNotNull(feature.TryApplySignOut(42, 11, "new"));
    }

    private static IGameConnection CreateConnection(IContactList<Friend> contacts)
    {
        var connection = Substitute.For<IGameConnection>();
        var features = new FeatureCollection();
        var character = Substitute.For<ICharacter>();
        character.Friends.Returns(contacts);
        character.Ignores.Returns(new ContactList<Ignore>());
        features.Set<IContactsFeature>(new WorldContactsFeature(character));
        connection.Features.Returns(features);
        connection.SendMessage(Arg.Any<RaidoMessage>()).Returns(Task.CompletedTask);
        return connection;
    }

    private static IMapper CreateMapper()
    {
        var mapper = Substitute.For<IMapper>();
        mapper.Map<ContactDto>(Arg.Any<Hagalaz.Contacts.Messages.Model.ContactDto>()).Returns(new ContactDto
        {
            MasterId = 42,
            DisplayName = "Friend",
            WorldId = 1
        });
        return mapper;
    }

    private static ConsumeContext<T> CreateContext<T>(T message) where T : class
    {
        var context = Substitute.For<ConsumeContext<T>>();
        context.Message.Returns(message);
        return context;
    }

    private static Hagalaz.Contacts.Messages.Model.ContactDto CreateContact() => new()
    {
        MasterId = 42,
        DisplayName = "Friend",
        WorldId = 1,
        WorldName = "World 1"
    };

    private static async IAsyncEnumerable<IGameConnection> One(
        IGameConnection connection,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return connection;
        await Task.CompletedTask;
    }

}
