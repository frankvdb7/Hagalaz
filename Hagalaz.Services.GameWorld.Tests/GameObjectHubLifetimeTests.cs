using System.IO.Pipelines;
using System.Security.Claims;
using Hagalaz.Game.Abstractions.Builders.Location;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Builders;
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
public sealed class GameObjectHubLifetimeTests
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
    public async Task GameObjectClick_UsesCharacterScopeAfterRaidoMessageScopeIsDisposed()
    {
        var serviceInstances = new List<TrackingGameObjectService>();
        var gameObjectScript = Substitute.For<IGameObjectScript>();
        var gameObject = Substitute.For<IGameObject>();
        gameObject.Id.Returns(42);
        gameObject.Script.Returns(gameObjectScript);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILocationBuilder, LocationBuilder>();
        services.AddSingleton<ScopeProbe>();
        services.AddScoped<ScopeProbeFilter>();
        services.AddScoped<IGameObjectService>(_ =>
        {
            var service = new TrackingGameObjectService(gameObject);
            serviceInstances.Add(service);
            return service;
        });
        services.AddRaidoServer().AddHub<GameObjectHub>();
        services.Configure<RaidoHubOptions<GameObjectHub>>(options => options.AddFilter<GameObjectHub, ScopeProbeFilter>());

        using var provider = services.BuildServiceProvider();
        using var characterScope = provider.CreateScope();
        var scheduler = new RsTaskService(NullLogger<RsTaskService>.Instance);
        var character = CreateCharacter(characterScope.ServiceProvider, scheduler);
        var connection = CreateConnection(character);

        await provider.GetRequiredService<IRaidoDispatcher>().DispatchMessageAsync(
            connection,
            new GameObjectClickMessage
            {
                Id = 42,
                AbsX = 3200,
                AbsY = 3200,
                ClickType = GameObjectClickType.Option1Click,
                ForceRun = false
            });

        Assert.IsTrue(provider.GetRequiredService<ScopeProbe>().MessageScopeDisposed);
        Assert.HasCount(0, serviceInstances);
        gameObjectScript.DidNotReceive().OnCharacterClick(Arg.Any<ICharacter>(), Arg.Any<GameObjectClickType>(), Arg.Any<bool>());

        scheduler.Tick();

        Assert.HasCount(1, serviceInstances);
        Assert.IsFalse(serviceInstances[0].IsDisposed);
        Assert.AreSame(characterScope.ServiceProvider.GetRequiredService<IGameObjectService>(), serviceInstances[0]);
        gameObjectScript.Received(1).OnCharacterClick(character, GameObjectClickType.Option1Click, false);
    }

    private RaidoHubConnectionContext CreateConnection(ICharacter character)
    {
        var features = new FeatureCollection();
        features.Set<IConnectionUserFeature>(new ConnectionUserFeature
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([], "test"))
        });
        var rawConnection = Substitute.For<ConnectionContext>();
        rawConnection.ConnectionId.Returns($"game-object-hub-test-{_connections.Count}");
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

    private static ICharacter CreateCharacter(IServiceProvider serviceProvider, ICreatureTaskService scheduler)
    {
        var character = Substitute.For<ICharacter>();
        var location = Location.Create(3200, 3200, 1, 7);
        var viewport = Substitute.For<IViewport>();
        character.IsDestroyed.Returns(false);
        character.Location.Returns(location);
        character.ServiceProvider.Returns(serviceProvider);
        character.Viewport.Returns(viewport);
        viewport.InBounds(Arg.Any<ILocation>()).Returns(true);
        character.QueueTask(Arg.Any<ITaskItem>()).Returns(callInfo =>
        {
            scheduler.Schedule(callInfo.Arg<ITaskItem>());
            return Substitute.For<IRsTaskHandle>();
        });
        return character;
    }

    private sealed class TrackingGameObjectService(IGameObject gameObject) : IGameObjectService, IDisposable
    {
        public bool IsDisposed { get; private set; }

        public IEnumerable<IGameObject> FindByLocation(ILocation location)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return [gameObject];
        }

        public Task<IGameObjectDefinition> FindGameObjectDefinitionById(int objectId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IGameObjectDefinition>(null!);

        public int GetObjectsCount() => 0;

        public void UpdateGameObject(GameObjectUpdate gameObjectUpdate) { }

        public void AnimateGameObject(IGameObject gameObject, IAnimation animation) { }

        public void Dispose() => IsDisposed = true;
    }

    private sealed class ScopeProbe
    {
        public bool MessageScopeDisposed { get; set; }
    }

    private sealed class ScopeProbeFilter(ScopeProbe probe) : IRaidoHubFilter, IDisposable
    {
        public ValueTask<object?> InvokeMethodAsync(RaidoHubInvocationContext context, Func<RaidoHubInvocationContext, ValueTask<object?>> next)
        {
            return next(context);
        }

        public void Dispose() => probe.MessageScopeDisposed = true;
    }

    private sealed class ConnectionUserFeature : IConnectionUserFeature
    {
        public ClaimsPrincipal? User { get; set; }
    }
}
