using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Contacts.Messages;
using Hagalaz.Contacts.Messages.Model;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Messages;
using Hagalaz.Game.Messages.Mediator;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Mediator.Consumers;
using Hagalaz.Services.GameWorld.Network.Model;
using Hagalaz.Services.GameWorld.Profiles;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.AspNetCore.Http.Features;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class WorldSignInCommandConsumerTests
{
    [TestMethod]
    public async Task Consume_WhenRegistrationFails_AbortsConnectionForNormalDisconnectCleanup()
    {
        var failure = new InvalidOperationException("Map initialization failed.");
        var character = Substitute.For<ICharacter>();
        var session = Substitute.For<IGameSession>();
        var viewport = Substitute.For<IViewport>();
        viewport.VisibleRegions.Returns(Array.Empty<IMapRegion>());
        session.ConnectionId.Returns("connection");
        character.MasterId.Returns(42u);
        character.Session.Returns(session);
        character.Viewport.Returns(viewport);
        character.When(value => value.OnRegistered()).Do(_ => throw failure);

        var connectionTerminator = Substitute.For<IGameSessionConnectionTerminator>();
        var publishEndpoint = Substitute.For<IBus>();
        using var schedulerProvider = new ServiceCollection().BuildServiceProvider();
        using var scheduler = CreateScheduler(schedulerProvider);
        var consumer = CreateConsumer(publishEndpoint, scheduler, connectionTerminator, contactsClient: CreateContactsClient());

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => consumer.Consume(CreateContext(new WorldSignInCommand(character))));

        Assert.AreSame(failure, exception);
        character.DidNotReceive().Destroy();
        connectionTerminator.Received(1).Abort(session);
        await publishEndpoint.DidNotReceive().Publish(
            Arg.Any<WorldUserSignOutMessage>(),
            Arg.Any<CancellationToken>());
        await publishEndpoint.DidNotReceive().Publish(
            Arg.Any<GetContactsRequest>(),
            Arg.Any<CancellationToken>());
        await publishEndpoint.DidNotReceive().Publish(
            Arg.Any<WorldUserSignInMessage>(),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Consume_WhenRegistrationSucceeds_PublishesContactsAndWorldPresenceOnce()
    {
        var character = Substitute.For<ICharacter>();
        var session = Substitute.For<IGameSession>();
        var viewport = Substitute.For<IViewport>();
        viewport.VisibleRegions.Returns(Array.Empty<IMapRegion>());
        session.ConnectionId.Returns("world-connection");
        session.MasterId.Returns(42u);
        session.SessionGeneration.Returns(2L);
        character.MasterId.Returns(42u);
        character.Session.Returns(session);
        character.Viewport.Returns(viewport);

        var publishEndpoint = Substitute.For<IBus>();
        var contactsClient = CreateContactsClient();
        using var schedulerProvider = new ServiceCollection().BuildServiceProvider();
        using var scheduler = CreateScheduler(schedulerProvider);
        var identity = new WorldInstanceIdentity();
        var consumer = CreateConsumer(
            publishEndpoint,
            scheduler,
            Substitute.For<IGameSessionConnectionTerminator>(),
            identity,
            contactsClient);

        await consumer.Consume(CreateContext(new WorldSignInCommand(character)));

        character.Received(1).OnRegistered();
        await contactsClient.Received(1).GetResponse<GetContactsResponse>(
            Arg.Is<GetContactsRequest>(message => message != null && message.MasterId == 42u && message.SessionGeneration == 2L && message.ConnectionId == "world-connection"),
            Arg.Any<CancellationToken>(),
            Arg.Any<RequestTimeout>());
        await publishEndpoint.DidNotReceive().Publish(Arg.Any<GetContactsRequest>(), Arg.Any<CancellationToken>());
        await publishEndpoint.Received(1).Publish(
            Arg.Is<WorldUserSignInMessage>(message => message != null && message.MasterId == 42u && message.WorldId == 1 && message.WorldInstanceId == identity.InstanceId && message.WorldGeneration == identity.Generation && message.SessionGeneration == 2L && message.ConnectionId == "world-connection"),
            Arg.Any<CancellationToken>());
        character.DidNotReceive().Destroy();
    }

    [TestMethod]
    public async Task Consume_WaitsForAllVisibleRegionsBeforeRegisteringCharacter()
    {
        var loadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseLoad = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var loaded = false;
        var loader = Substitute.For<IMapRegionLoader>();
        loader.LoadAsync(Arg.Any<IMapRegion>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                loadStarted.TrySetResult();
                await releaseLoad.Task;
                loaded = true;
            });
        var region = Substitute.For<IMapRegion>();
        region.Id.Returns(1);
        region.BaseLocation.Returns(Location.Create(64, 0, 0, 0));
        region.State.Returns(_ => loaded ? MapRegionState.Ready : MapRegionState.Initializing);
        var viewport = Substitute.For<IViewport>();
        viewport.VisibleRegions.Returns(new[] { region });
        var character = Substitute.For<ICharacter>();
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("world-connection");
        session.MasterId.Returns(42u);
        session.SessionGeneration.Returns(2L);
        character.MasterId.Returns(42u);
        character.Session.Returns(session);
        character.Viewport.Returns(viewport);
        var publishEndpoint = Substitute.For<IBus>();
        var contactsClient = CreateContactsClient();
        var terminator = Substitute.For<IGameSessionConnectionTerminator>();
        using var schedulerProvider = new ServiceCollection()
            .AddScoped(_ => loader)
            .BuildServiceProvider();
        using var scheduler = CreateScheduler(schedulerProvider);
        await scheduler.StartAsync(CancellationToken.None);
        var consumer = CreateConsumer(publishEndpoint, scheduler, terminator, contactsClient: contactsClient);

        var consumeTask = consumer.Consume(CreateContext(new WorldSignInCommand(character)));
        await loadStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.IsFalse(consumeTask.IsCompleted);
        character.DidNotReceive().OnRegistered();
        viewport.Received(1).RebuildView();

        releaseLoad.TrySetResult();
        await consumeTask.WaitAsync(TimeSpan.FromSeconds(1));
        await scheduler.StopAsync(CancellationToken.None);

        character.Received(1).OnRegistered();
        await contactsClient.Received(1).GetResponse<GetContactsResponse>(
            Arg.Is<GetContactsRequest>(message => message != null && message.MasterId == 42u && message.SessionGeneration == 2L && message.ConnectionId == "world-connection"),
            Arg.Any<CancellationToken>(),
            Arg.Any<RequestTimeout>());
        await publishEndpoint.Received(1).Publish(
            Arg.Is<WorldUserSignInMessage>(message => message != null && message.MasterId == 42u && message.WorldId == 1 && message.WorldInstanceId != string.Empty && message.WorldGeneration != 0 && message.SessionGeneration == 2L && message.ConnectionId == "world-connection"),
            Arg.Any<CancellationToken>());
        terminator.DidNotReceive().Abort(Arg.Any<IGameSession>());
    }

    [TestMethod]
    public async Task Consume_WhenVisibleRegionDoesNotBecomeReady_AbortsAndDisconnectsWithoutRetry()
    {
        var loader = Substitute.For<IMapRegionLoader>();
        loader.LoadAsync(Arg.Any<IMapRegion>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var region = Substitute.For<IMapRegion>();
        region.Id.Returns(1);
        region.BaseLocation.Returns(Location.Create(64, 0, 0, 0));
        region.State.Returns(MapRegionState.Initializing);
        var viewport = Substitute.For<IViewport>();
        viewport.VisibleRegions.Returns(new[] { region });
        var character = Substitute.For<ICharacter>();
        var session = Substitute.For<IGameSession>();
        session.ConnectionId.Returns("connection");
        character.MasterId.Returns(42u);
        character.Session.Returns(session);
        character.Viewport.Returns(viewport);
        var terminator = Substitute.For<IGameSessionConnectionTerminator>();
        var publishEndpoint = Substitute.For<IBus>();
        var contactsClient = CreateContactsClient();
        using var schedulerProvider = new ServiceCollection()
            .AddScoped(_ => loader)
            .BuildServiceProvider();
        using var scheduler = CreateScheduler(schedulerProvider);
        await scheduler.StartAsync(CancellationToken.None);
        var consumer = CreateConsumer(publishEndpoint, scheduler, terminator, contactsClient: contactsClient);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => consumer.Consume(CreateContext(new WorldSignInCommand(character))));
        await scheduler.StopAsync(CancellationToken.None);

        await loader.Received(1).LoadAsync(region, Arg.Any<CancellationToken>());
        character.DidNotReceive().OnRegistered();
        terminator.Received(1).Abort(session);
        await contactsClient.DidNotReceive().GetResponse<GetContactsResponse>(
            Arg.Any<GetContactsRequest>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>());
        await publishEndpoint.DidNotReceive().Publish(
            Arg.Any<WorldUserSignInMessage>(),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Consume_WhenContactsRequestTimesOut_ContinuesLoginAndCompletesSnapshotGate()
    {
        await Consume_WhenContactsRequestFails_ContinuesLoginAndCompletesSnapshotGate(
            new RequestTimeoutException("Contacts timed out."));
    }

    [TestMethod]
    public async Task Consume_WhenContactsRequestFaults_ContinuesLoginAndCompletesSnapshotGate()
    {
        await Consume_WhenContactsRequestFails_ContinuesLoginAndCompletesSnapshotGate(
            new RequestFaultException());
    }

    [TestMethod]
    public async Task Consume_WhenSignInCancellationIsRequested_PropagatesCancellationAfterClosingSnapshotWindow()
    {
        var character = Substitute.For<ICharacter>();
        var session = Substitute.For<IGameSession>();
        var viewport = Substitute.For<IViewport>();
        viewport.VisibleRegions.Returns(Array.Empty<IMapRegion>());
        session.ConnectionId.Returns("world-connection");
        session.MasterId.Returns(42u);
        session.SessionGeneration.Returns(2L);
        character.MasterId.Returns(42u);
        character.Session.Returns(session);
        character.Viewport.Returns(viewport);

        var contacts = new LobbyContactsFeature();
        var connection = CreateConnection(contacts);
        var connectionService = Substitute.For<IGameConnectionService>();
        connectionService.FindById("world-connection").Returns(Task.FromResult<IGameConnection?>(connection));
        using var cancellationSource = new CancellationTokenSource();
        var contactsClient = Substitute.For<IRequestClient<GetContactsRequest>>();
        contactsClient.GetResponse<GetContactsResponse>(
                Arg.Any<GetContactsRequest>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .Returns(_ => Task.FromCanceled<Response<GetContactsResponse>>(cancellationSource.Token));
        var terminator = Substitute.For<IGameSessionConnectionTerminator>();
        var consumer = CreateConsumer(
            Substitute.For<IBus>(),
            CreateScheduler(new ServiceCollection().BuildServiceProvider()),
            terminator,
            contactsClient: contactsClient,
            connectionService: connectionService);

        cancellationSource.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => consumer.Consume(CreateContext(new WorldSignInCommand(character), cancellationSource.Token)));
        await contacts.WaitForInitialSnapshotAsync(CancellationToken.None);
        terminator.DidNotReceive().Abort(Arg.Any<IGameSession>());
    }

    [TestMethod]
    public async Task Consume_WhenPresenceChangesDuringContactsRequest_SendsReconciledFriendSnapshot()
    {
        var character = Substitute.For<ICharacter>();
        var session = Substitute.For<IGameSession>();
        var viewport = Substitute.For<IViewport>();
        viewport.VisibleRegions.Returns(Array.Empty<IMapRegion>());
        session.ConnectionId.Returns("world-connection");
        session.MasterId.Returns(42u);
        session.SessionGeneration.Returns(2L);
        character.MasterId.Returns(42u);
        character.Session.Returns(session);
        character.Viewport.Returns(viewport);

        var contacts = new LobbyContactsFeature();
        var connection = CreateConnection(contacts);
        var connectionService = Substitute.For<IGameConnectionService>();
        connectionService.FindById("world-connection").Returns(Task.FromResult<IGameConnection?>(connection));

        var response = Substitute.For<Response<GetContactsResponse>>();
        response.Message.Returns(new GetContactsResponse
        {
            MasterId = 42,
            SessionGeneration = 2,
            ConnectionId = "world-connection",
            Friends = [CreateContactMessageDto()],
            Ignores = []
        });
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseResponse = new TaskCompletionSource<Response<GetContactsResponse>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var contactsClient = Substitute.For<IRequestClient<GetContactsRequest>>();
        contactsClient.GetResponse<GetContactsResponse>(
                Arg.Any<GetContactsRequest>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .Returns(_ =>
            {
                requestStarted.TrySetResult();
                return releaseResponse.Task;
            });
        var publishEndpoint = Substitute.For<IBus>();
        var mapper = new MapperConfiguration(
            configuration => configuration.AddProfile<ContactsProfile>(),
            LoggerFactory.Create(_ => { })).CreateMapper();
        using var schedulerProvider = new ServiceCollection().BuildServiceProvider();
        using var scheduler = CreateScheduler(schedulerProvider);
        var consumer = CreateConsumer(
            publishEndpoint,
            scheduler,
            Substitute.For<IGameSessionConnectionTerminator>(),
            contactsClient: contactsClient,
            connectionService: connectionService,
            mapper: mapper);

        var consumeTask = consumer.Consume(CreateContext(new WorldSignInCommand(character)));
        await requestStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.IsNull(contacts.TryApplySignIn(42, 10, "friend-connection", 5, "World 5"));
        releaseResponse.TrySetResult(response);
        await consumeTask.WaitAsync(TimeSpan.FromSeconds(1));

        var receivedMessages = session.ReceivedCalls()
            .SelectMany(call => call.GetArguments())
            .OfType<FriendsListMessage>()
            .ToList();
        Assert.HasCount(1, receivedMessages, string.Join(", ", session.ReceivedCalls().Select(call => call.GetMethodInfo().Name)));
        var friendsMessage = receivedMessages[0];
        var friend = friendsMessage.Friends.Single();
        Assert.AreEqual(42, friend.MasterId);
        Assert.AreEqual(5, friend.WorldId);
        Assert.AreEqual("World 5", friend.WorldName);
    }

    private async Task Consume_WhenContactsRequestFails_ContinuesLoginAndCompletesSnapshotGate(Exception failure)
    {
        var character = Substitute.For<ICharacter>();
        var session = Substitute.For<IGameSession>();
        var viewport = Substitute.For<IViewport>();
        viewport.VisibleRegions.Returns(Array.Empty<IMapRegion>());
        session.ConnectionId.Returns("world-connection");
        session.MasterId.Returns(42u);
        session.SessionGeneration.Returns(2L);
        character.MasterId.Returns(42u);
        character.Session.Returns(session);
        character.Viewport.Returns(viewport);

        var contacts = new LobbyContactsFeature();
        var connection = CreateConnection(contacts);
        var connectionService = Substitute.For<IGameConnectionService>();
        connectionService.FindById("world-connection").Returns(Task.FromResult<IGameConnection?>(connection));
        var contactsClient = Substitute.For<IRequestClient<GetContactsRequest>>();
        contactsClient.GetResponse<GetContactsResponse>(
                Arg.Any<GetContactsRequest>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .Returns(Task.FromException<Response<GetContactsResponse>>(failure));
        var terminator = Substitute.For<IGameSessionConnectionTerminator>();
        var consumer = CreateConsumer(
            Substitute.For<IBus>(),
            CreateScheduler(new ServiceCollection().BuildServiceProvider()),
            terminator,
            contactsClient: contactsClient,
            connectionService: connectionService);

        await consumer.Consume(CreateContext(new WorldSignInCommand(character)));
        await contacts.WaitForInitialSnapshotAsync(CancellationToken.None);

        character.Received(1).OnRegistered();
        terminator.DidNotReceive().Abort(Arg.Any<IGameSession>());
    }

    private static WorldSignInCommandConsumer CreateConsumer(
        IBus publishEndpoint,
        IMapRegionLoadScheduler scheduler,
        IGameSessionConnectionTerminator connectionTerminator,
        WorldInstanceIdentity? identity = null,
        IRequestClient<GetContactsRequest>? contactsClient = null,
        IGameConnectionService? connectionService = null,
        IMapper? mapper = null) =>
        new(
            publishEndpoint,
            contactsClient ?? CreateContactsClient(),
            Options.Create(new WorldOptions { Id = 1 }),
            scheduler,
            connectionTerminator,
            connectionService ?? Substitute.For<IGameConnectionService>(),
            mapper ?? Substitute.For<AutoMapper.IMapper>(),
            identity ?? new WorldInstanceIdentity(),
            NullLogger<WorldSignInCommandConsumer>.Instance);

    private static IRequestClient<GetContactsRequest> CreateContactsClient()
    {
        var client = Substitute.For<IRequestClient<GetContactsRequest>>();
        var response = Substitute.For<Response<GetContactsResponse>>();
        response.Message.Returns(new GetContactsResponse
        {
            MasterId = 42,
            SessionGeneration = 2,
            ConnectionId = "world-connection",
            Friends = [],
            Ignores = []
        });
        client.GetResponse<GetContactsResponse>(
                Arg.Any<GetContactsRequest>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .Returns(Task.FromResult(response));
        return client;
    }

    private static IGameConnection CreateConnection(IContactsFeature contacts)
    {
        var connection = Substitute.For<IGameConnection>();
        var features = new FeatureCollection();
        features.Set<IContactsFeature>(contacts);
        connection.Features.Returns(features);
        return connection;
    }

    private static ContactDto CreateContactMessageDto() => new()
    {
        MasterId = 42,
        DisplayName = "Friend",
        Settings = new ContactSettingsDto(ContactAvailability.Everyone)
    };

    private static MapRegionLoadScheduler CreateScheduler(ServiceProvider provider) =>
        new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<MapRegionLoadScheduler>.Instance);

    private static ConsumeContext<WorldSignInCommand> CreateContext(
        WorldSignInCommand message,
        CancellationToken cancellationToken = default)
    {
        var context = Substitute.For<ConsumeContext<WorldSignInCommand>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(cancellationToken);
        return context;
    }
}
