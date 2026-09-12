using System.IO.Pipelines;
using System.Security.Claims;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Hubs;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Raido.Common.Protocol;
using Raido.Server;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CharacterHubTests
{
    private readonly List<RaidoHubConnectionContext> _connections = [];
    private readonly List<(Pipe Input, Pipe Output)> _transports = [];

    [TestCleanup]
    public async Task CleanupConnections()
    {
        foreach (var connection in _connections)
        {
            connection.Abort();
            await connection.CleanupAsync();
        }

        foreach (var (input, output) in _transports)
        {
            input.Reader.Complete();
            input.Writer.Complete();
            output.Reader.Complete();
            output.Writer.Complete();
        }
    }

    [TestMethod]
    public async Task OnMovement_QueuesGameplayUntilTheGameTaskRuns()
    {
        using var provider = CreateProvider();
        var character = CreateCharacter(out var eventManager, out var queuedTasks);
        var connection = CreateConnection(character);
        var message = new MovementMessage { AbsX = 3201, AbsY = 3202, ForceRun = true };

        await provider.GetRequiredService<IRaidoDispatcher>().DispatchMessageAsync(connection, message);

        Assert.HasCount(1, queuedTasks);
        eventManager.DidNotReceive().SendEvent(Arg.Any<IEvent>());
        character.DidNotReceive().Interrupt(Arg.Any<object>());

        character.Location.Returns(Location.Create(3200, 3200, 3, 9));
        queuedTasks[0].Tick();

        var walkEvent = eventManager.ReceivedCalls()
            .Select(call => call.GetArguments().Single())
            .OfType<WalkAllowEvent>()
            .Single();
        Assert.AreEqual(3201, walkEvent.TargetLocation.X);
        Assert.AreEqual(3202, walkEvent.TargetLocation.Y);
        Assert.AreEqual(3, walkEvent.TargetLocation.Z);
        Assert.AreEqual(9, walkEvent.TargetLocation.Dimension);
        character.Received(1).Interrupt(Arg.Any<object>());
        Assert.AreEqual(MovementType.Run, character.Movement.MovementType);
        Assert.HasCount(2, queuedTasks);
    }

    [TestMethod]
    public async Task OnMovement_RejectedByWalkAllowDoesNotInterruptOrQueueReachTask()
    {
        using var provider = CreateProvider();
        var character = CreateCharacter(out var eventManager, out var queuedTasks);
        eventManager.SendEvent(Arg.Any<IEvent>()).Returns(call => call.Arg<IEvent>() is not WalkAllowEvent);
        var connection = CreateConnection(character);

        await provider.GetRequiredService<IRaidoDispatcher>().DispatchMessageAsync(
            connection,
            new MovementMessage { AbsX = 3201, AbsY = 3202, ForceRun = true });

        queuedTasks[0].Tick();

        character.DidNotReceive().Interrupt(Arg.Any<object>());
        Assert.HasCount(1, queuedTasks);
    }

    [TestMethod]
    public async Task OnMovement_DoesNothingWhenCharacterIsDestroyedBeforeExecution()
    {
        using var provider = CreateProvider();
        var character = CreateCharacter(out var eventManager, out var queuedTasks);
        var connection = CreateConnection(character);

        await provider.GetRequiredService<IRaidoDispatcher>().DispatchMessageAsync(
            connection,
            new MovementMessage { AbsX = 3201, AbsY = 3202, ForceRun = false });

        character.IsDestroyed.Returns(true);
        queuedTasks[0].Tick();

        eventManager.DidNotReceive().SendEvent(Arg.Any<IEvent>());
        character.DidNotReceive().Interrupt(Arg.Any<object>());
        Assert.HasCount(1, queuedTasks);
    }

    [TestMethod]
    public async Task OnPublicChat_QueuesChatDecisionUntilTheGameTaskRuns()
    {
        using var provider = CreateProvider();
        var character = CreateCharacter(out var eventManager, out var queuedTasks);
        var connection = CreateConnection(character);

        await provider.GetRequiredService<IRaidoDispatcher>().DispatchMessageAsync(
            connection,
            new PublicChatMessage { Text = "hello", TextAnimation = 0, TextColor = 0 });

        Assert.HasCount(1, queuedTasks);
        eventManager.DidNotReceive().SendEvent(Arg.Any<IEvent>());

        queuedTasks[0].Tick();

        eventManager.Received(1).SendEvent(Arg.Is<ChatAllowEvent>(value => value.Text == "hello"));
    }

    private ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<ICharacterService>());
        services.AddSingleton(Substitute.For<IAuthenticationService>());
        services.AddRaidoServer().AddHub<CharacterHub>();
        return services.BuildServiceProvider();
    }

    private RaidoHubConnectionContext CreateConnection(ICharacter character)
    {
        var features = new FeatureCollection();
        features.Set<IConnectionUserFeature>(new ConnectionUserFeature
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([], "test"))
        });
        var rawConnection = Substitute.For<ConnectionContext>();
        rawConnection.ConnectionId.Returns($"character-hub-test-{_connections.Count}");
        rawConnection.Features.Returns(features);
        var input = new Pipe();
        var output = new Pipe();
        var transport = Substitute.For<IDuplexPipe>();
        transport.Input.Returns(input.Reader);
        transport.Output.Returns(output.Writer);
        rawConnection.Transport.Returns(transport);
        rawConnection.ConnectionClosed.Returns(CancellationToken.None);
        _transports.Add((input, output));

        var options = new RaidoConnectionContextOptions();
        var tcpConnection = new RaidoTcpConnectionContext(options, NullLoggerFactory.Instance);
        Assert.IsTrue(tcpConnection.TryAttachPhysicalConnection(rawConnection));
        var connection = new RaidoHubConnectionContext(
            tcpConnection,
            options,
            Substitute.For<IRaidoProtocol>(),
            NullLoggerFactory.Instance,
            TimeProvider.System);
        connection.Features.Set<ICharacterFeature>(new CharacterFeature { Character = character });
        _connections.Add(connection);
        return connection;
    }

    private static ICharacter CreateCharacter(out IEventManager eventManager, out List<ITaskItem> queuedTasks)
    {
        eventManager = Substitute.For<IEventManager>();
        eventManager.SendEvent(Arg.Any<IEvent>()).Returns(true);
        var tasks = new List<ITaskItem>();
        queuedTasks = tasks;
        var character = Substitute.For<ICharacter>();
        var location = Location.Create(3200, 3200, 1, 7);
        var movement = Substitute.For<IMovement>();
        var viewport = Substitute.For<IViewport>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var pathFinderProvider = Substitute.For<IPathFinderProvider>();
        pathFinderProvider.Smart.Returns(Substitute.For<ISmartPathFinder>());

        character.IsDestroyed.Returns(false);
        character.Location.Returns(location);
        character.Movement.Returns(movement);
        character.Viewport.Returns(viewport);
        character.ServiceProvider.Returns(serviceProvider);
        character.EventManager.Returns(eventManager);
        viewport.VisibleCharacters.Returns(Array.Empty<ICharacter>());
        serviceProvider.GetService(typeof(IPathFinderProvider)).Returns(pathFinderProvider);
        character.QueueTask(Arg.Do<ITaskItem>(task => tasks.Add(task)))
            .Returns(Substitute.For<IRsTaskHandle>());
        return character;
    }

    private sealed class ConnectionUserFeature : IConnectionUserFeature
    {
        public ClaimsPrincipal? User { get; set; }
    }
}
