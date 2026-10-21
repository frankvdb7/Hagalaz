using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Contacts.Messages;
using Hagalaz.Game.Abstractions.Features.Chat;
using Hagalaz.Game.Abstractions.Features.FriendsChat;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Messages;
using Hagalaz.Game.Messages.Mediator;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Mediator.Consumers;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Network.Model;
using Hagalaz.Services.GameWorld.Services;
using MassTransit;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class LobbySignInCommandConsumerTests
{
    [TestMethod]
    public async Task Consume_WhenContactsSnapshotLoads_ReplacesListsAndClosesObservationWindow()
    {
        var contacts = new ContactsFeature();
        contacts.Friends.Add(CreateFriend(42));
        contacts.Ignores.Add(new Ignore { MasterId = 43 });
        var contactsClient = CreateContactsClient(() => Task.FromResult(CreateResponse(new GetContactsResponse
        {
            MasterId = 42,
            SessionGeneration = 7,
            ConnectionId = "lobby-connection",
            Friends = [],
            Ignores = []
        })));
        var fixture = CreateFixture(contacts, contactsClient);

        await fixture.Consumer.Consume(fixture.Context);

        await contacts.WaitForInitialSnapshotAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(1));
        await contactsClient.Received(1).GetResponse<GetContactsResponse>(
            Arg.Is<GetContactsRequest>(request =>
                request.MasterId == 42 &&
                request.SessionGeneration == 7 &&
                request.ConnectionId == "lobby-connection"),
            Arg.Any<CancellationToken>(),
            Arg.Any<RequestTimeout>());
        await fixture.Mediator.Received(1).SendAsync(
            Arg.Is<SendWorldInfoCommand>(command => command.Session == fixture.Session));
        await fixture.PublishEndpoint.Received(1).Publish(
            Arg.Is<LobbyUserSignInMessage>(message =>
                message.MasterId == 42 &&
                message.WorldId == 1 &&
                message.WorldInstanceId == fixture.Identity.InstanceId &&
                message.WorldGeneration == fixture.Identity.Generation &&
                message.SessionGeneration == 7 &&
                message.ConnectionId == "lobby-connection"),
            Arg.Any<CancellationToken>());
        Assert.IsNull(contacts.Friends.Get(42));
        Assert.IsNull(contacts.Ignores.Get(43));

        Assert.IsNull(contacts.TryApplySignIn(99, 1, "unknown"));
        contacts.AddFriend(CreateFriend(99), null);
        Assert.IsNull(contacts.TryApplySignOut(99, 1, "unknown"));
    }

    [TestMethod]
    public async Task Consume_WhenContactsRequestTimesOut_ContinuesSignInAndCompletesSnapshotGate() =>
        await Consume_WhenContactsRequestFails_ContinuesSignInAndCompletesSnapshotGate(
            new RequestTimeoutException("Contacts timed out."));

    [TestMethod]
    public async Task Consume_WhenContactsRequestFaults_ContinuesSignInAndCompletesSnapshotGate() =>
        await Consume_WhenContactsRequestFails_ContinuesSignInAndCompletesSnapshotGate(
            new RequestFaultException());

    [TestMethod]
    public async Task Consume_WhenContactsRequestIsCanceled_PropagatesCancellationAfterClosingSnapshotWindow()
    {
        using var cancellationSource = new CancellationTokenSource();
        var contacts = new ContactsFeature();
        var contactsClient = CreateContactsClient(() =>
        {
            Assert.IsNull(contacts.TryApplySignIn(99, 1, "unknown"));
            return Task.FromCanceled<Response<GetContactsResponse>>(cancellationSource.Token);
        });
        cancellationSource.Cancel();
        var fixture = CreateFixture(contacts, contactsClient, cancellationSource.Token);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => fixture.Consumer.Consume(fixture.Context));
        await contacts.WaitForInitialSnapshotAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(1));

        contacts.AddFriend(CreateFriend(99), null);
        Assert.IsFalse(contacts.GetPresence(99).IsOnline);
    }

    private async Task Consume_WhenContactsRequestFails_ContinuesSignInAndCompletesSnapshotGate(Exception failure)
    {
        var contacts = new ContactsFeature();
        var existingFriend = CreateFriend(42);
        contacts.Friends.Add(existingFriend);
        var contactsClient = CreateContactsClient(() => Task.FromException<Response<GetContactsResponse>>(failure));
        var fixture = CreateFixture(contacts, contactsClient);

        await fixture.Consumer.Consume(fixture.Context);
        await contacts.WaitForInitialSnapshotAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(1));

        Assert.AreSame(existingFriend, contacts.Friends.Get(42));
        await fixture.Mediator.Received(1).SendAsync(
            Arg.Is<SendWorldInfoCommand>(command => command.Session == fixture.Session));
        await fixture.PublishEndpoint.Received(1).Publish(
            Arg.Is<LobbyUserSignInMessage>(message => message.MasterId == 42),
            Arg.Any<CancellationToken>());
    }

    private static (
        LobbySignInCommandConsumer Consumer,
        ConsumeContext<LobbySignInCommand> Context,
        IGameSession Session,
        IBus PublishEndpoint,
        IScopedGameMediator Mediator,
        WorldInstanceIdentity Identity) CreateFixture(
            ContactsFeature contacts,
            IRequestClient<GetContactsRequest> contactsClient,
            CancellationToken cancellationToken = default)
    {
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("lobby-connection");
        session.MasterId.Returns(42u);
        session.SessionGeneration.Returns(7L);

        var connection = Substitute.For<IGameConnection>();
        var features = new FeatureCollection();
        features.Set<IContactsFeature>(contacts);
        connection.Features.Returns(features);
        var connectionService = Substitute.For<IGameConnectionService>();
        connectionService.FindById("lobby-connection")
            .Returns(Task.FromResult<IGameConnection?>(connection));

        var publishEndpoint = Substitute.For<IBus>();
        var mediator = Substitute.For<IScopedGameMediator>();
        mediator.SendAsync(Arg.Any<SendWorldInfoCommand>()).Returns(Task.CompletedTask);
        var identity = new WorldInstanceIdentity();
        var consumer = new LobbySignInCommandConsumer(
            publishEndpoint,
            contactsClient,
            mediator,
            connectionService,
            Options.Create(new WorldOptions { Id = 1 }),
            identity,
            Substitute.For<IMapper>(),
            NullLogger<LobbySignInCommandConsumer>.Instance);
        var context = Substitute.For<ConsumeContext<LobbySignInCommand>>();
        context.Message.Returns(new LobbySignInCommand(42, session));
        context.CancellationToken.Returns(cancellationToken);

        return (consumer, context, session, publishEndpoint, mediator, identity);
    }

    private static IRequestClient<GetContactsRequest> CreateContactsClient(
        Func<Task<Response<GetContactsResponse>>> responseFactory)
    {
        var client = Substitute.For<IRequestClient<GetContactsRequest>>();
        client.GetResponse<GetContactsResponse>(
                Arg.Any<GetContactsRequest>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .Returns(_ => responseFactory());
        return client;
    }

    private static Response<GetContactsResponse> CreateResponse(GetContactsResponse message)
    {
        var response = Substitute.For<Response<GetContactsResponse>>();
        response.Message.Returns(message);
        return response;
    }

    private static Friend CreateFriend(int masterId) => new()
    {
        MasterId = masterId,
        Rank = FriendsChatRank.Friend,
        Availability = Availability.Everyone,
        AreMutualFriends = true
    };
}
