using System.Collections.Generic;
using System.IO.Pipelines;
using System.Security.Claims;
using Hagalaz.Authorization.Constants;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Common.Events.Character.Packet;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Hubs;
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
public sealed class AdminHubAuthorizationTests
{
    private readonly List<RaidoHubConnectionContext> _connections = new();
    private readonly List<(Pipe Input, Pipe Output)> _transports = new();

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
    public async Task AdminHub_OnCommand_RejectsAuthenticatedNonAdmin()
    {
        using var provider = CreateProvider();
        var eventManager = Substitute.For<IEventManager>();
        var connection = CreateConnection(eventManager, Roles.Donator);

        await provider.GetRequiredService<IRaidoDispatcher>().DispatchMessageAsync(
            connection,
            new ConsoleCommandMessage { Command = "::dangerous-command" });

        eventManager.DidNotReceive().SendEvent(Arg.Any<IEvent>());
    }

    [TestMethod]
    public async Task AdminHub_OnCommand_AllowsModeratorAndPublishesCommandEvent()
    {
        using var provider = CreateProvider();
        var eventManager = Substitute.For<IEventManager>();
        var sentEvent = default(IEvent);
        eventManager.SendEvent(Arg.Do<IEvent>(e => sentEvent = e)).Returns(true);
        var connection = CreateConnection(eventManager, Roles.GameModerator);

        await provider.GetRequiredService<IRaidoDispatcher>().DispatchMessageAsync(
            connection,
            new ConsoleCommandMessage { Command = "::safe-command" });

        eventManager.Received(1).SendEvent(Arg.Any<IEvent>());
        var commandEvent = Assert.IsInstanceOfType<ConsoleCommandEvent>(sentEvent);
        Assert.AreEqual("::safe-command", commandEvent.Command);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRaidoServer().AddHub<AdminHub>();
        return services.BuildServiceProvider();
    }

    private RaidoHubConnectionContext CreateConnection(IEventManager eventManager, string role)
    {
        var features = new FeatureCollection();
        features.Set<IConnectionUserFeature>(new ConnectionUserFeature
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Role, role) }, "test"))
        });
        var rawConnection = Substitute.For<ConnectionContext>();
        rawConnection.ConnectionId.Returns("admin-hub-test");
        rawConnection.Features.Returns(features);
        var input = new Pipe();
        var output = new Pipe();
        var transport = Substitute.For<IDuplexPipe>();
        transport.Input.Returns(input.Reader);
        transport.Output.Returns(output.Writer);
        rawConnection.Transport.Returns(transport);
        rawConnection.ConnectionClosed.Returns(CancellationToken.None);
        _transports.Add((input, output));

        var connection = CreateHubConnection(rawConnection);
        connection.Features.Set<ICharacterFeature>(new CharacterFeature
        {
            Character = CreateCharacter(eventManager)
        });
        _connections.Add(connection);
        return connection;
    }

    private static RaidoHubConnectionContext CreateHubConnection(ConnectionContext physicalConnection)
    {
        var options = new RaidoConnectionContextOptions();
        var tcpConnection = new RaidoTcpConnectionContext(options, NullLoggerFactory.Instance);
        Assert.IsTrue(tcpConnection.TryAttachPhysicalConnection(physicalConnection));
        return new RaidoHubConnectionContext(
            tcpConnection,
            options,
            Substitute.For<IRaidoProtocol>(),
            NullLoggerFactory.Instance,
            TimeProvider.System);
    }

    private static ICharacter CreateCharacter(IEventManager eventManager)
    {
        var character = Substitute.For<ICharacter>();
        character.EventManager.Returns(eventManager);
        return character;
    }

    private sealed class ConnectionUserFeature : IConnectionUserFeature
    {
        public ClaimsPrincipal? User { get; set; }
    }

}
