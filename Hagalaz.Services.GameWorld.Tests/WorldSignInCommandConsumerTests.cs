using System;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Contacts.Messages;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Messages;
using Hagalaz.Game.Messages.Mediator;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Mediator.Consumers;
using Hagalaz.Services.GameWorld.Services;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
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
        var consumer = CreateConsumer(publishEndpoint, scheduler, connectionTerminator);

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
        session.SessionGeneration.Returns(2L);
        character.MasterId.Returns(42u);
        character.Session.Returns(session);
        character.Viewport.Returns(viewport);

        var publishEndpoint = Substitute.For<IBus>();
        using var schedulerProvider = new ServiceCollection().BuildServiceProvider();
        using var scheduler = CreateScheduler(schedulerProvider);
        var consumer = CreateConsumer(
            publishEndpoint,
            scheduler,
            Substitute.For<IGameSessionConnectionTerminator>());

        await consumer.Consume(CreateContext(new WorldSignInCommand(character)));

        character.Received(1).OnRegistered();
        await publishEndpoint.Received(1).Publish(
            Arg.Is<GetContactsRequest>(message => message != null && message.MasterId == 42u),
            Arg.Any<CancellationToken>());
        await publishEndpoint.Received(1).Publish(
            Arg.Is<WorldUserSignInMessage>(message => message != null && message.MasterId == 42u && message.WorldId == 1 && message.SessionGeneration == 2L && message.ConnectionId == "world-connection"),
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
        session.SessionGeneration.Returns(2L);
        character.MasterId.Returns(42u);
        character.Session.Returns(session);
        character.Viewport.Returns(viewport);
        var publishEndpoint = Substitute.For<IBus>();
        var terminator = Substitute.For<IGameSessionConnectionTerminator>();
        using var schedulerProvider = new ServiceCollection()
            .AddScoped(_ => loader)
            .BuildServiceProvider();
        using var scheduler = CreateScheduler(schedulerProvider);
        await scheduler.StartAsync(CancellationToken.None);
        var consumer = CreateConsumer(publishEndpoint, scheduler, terminator);

        var consumeTask = consumer.Consume(CreateContext(new WorldSignInCommand(character)));
        await loadStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.IsFalse(consumeTask.IsCompleted);
        character.DidNotReceive().OnRegistered();
        viewport.Received(1).RebuildView();

        releaseLoad.TrySetResult();
        await consumeTask.WaitAsync(TimeSpan.FromSeconds(1));
        await scheduler.StopAsync(CancellationToken.None);

        character.Received(1).OnRegistered();
        await publishEndpoint.Received(1).Publish(
            Arg.Is<GetContactsRequest>(message => message != null && message.MasterId == 42u),
            Arg.Any<CancellationToken>());
        await publishEndpoint.Received(1).Publish(
            Arg.Is<WorldUserSignInMessage>(message => message != null && message.MasterId == 42u && message.WorldId == 1 && message.SessionGeneration == 2L && message.ConnectionId == "world-connection"),
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
        character.IsDestroyed.Returns(false);
        var terminator = Substitute.For<IGameSessionConnectionTerminator>();
        var publishEndpoint = Substitute.For<IBus>();
        using var schedulerProvider = new ServiceCollection()
            .AddScoped(_ => loader)
            .BuildServiceProvider();
        using var scheduler = CreateScheduler(schedulerProvider);
        await scheduler.StartAsync(CancellationToken.None);
        var consumer = CreateConsumer(publishEndpoint, scheduler, terminator);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => consumer.Consume(CreateContext(new WorldSignInCommand(character))));
        await scheduler.StopAsync(CancellationToken.None);

        await loader.Received(1).LoadAsync(region, Arg.Any<CancellationToken>());
        character.DidNotReceive().OnRegistered();
        terminator.Received(1).Abort(session);
        await publishEndpoint.DidNotReceive().Publish(
            Arg.Any<GetContactsRequest>(),
            Arg.Any<CancellationToken>());
        await publishEndpoint.DidNotReceive().Publish(
            Arg.Any<WorldUserSignInMessage>(),
            Arg.Any<CancellationToken>());
    }

    private static WorldSignInCommandConsumer CreateConsumer(
        IBus publishEndpoint,
        IMapRegionLoadScheduler scheduler,
        IGameSessionConnectionTerminator connectionTerminator) =>
        new(
            publishEndpoint,
            Options.Create(new WorldOptions { Id = 1 }),
            scheduler,
            connectionTerminator,
            NullLogger<WorldSignInCommandConsumer>.Instance);

    private static MapRegionLoadScheduler CreateScheduler(ServiceProvider provider) =>
        new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<MapRegionLoadScheduler>.Instance);

    private static ConsumeContext<WorldSignInCommand> CreateContext(WorldSignInCommand message)
    {
        var context = Substitute.For<ConsumeContext<WorldSignInCommand>>();
        context.Message.Returns(message);
        return context;
    }
}
