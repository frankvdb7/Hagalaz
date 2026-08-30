using System.Reflection;
using System.Security.Claims;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Messages.Mediator;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Hubs;
using Hagalaz.Services.GameWorld.Model;
using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Hagalaz.Services.GameWorld.Network.Model;
using Hagalaz.Services.GameWorld.Network.Protocol;
using Hagalaz.Services.GameWorld.Providers;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using MassTransit;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NSubstitute;
using Raido.Common.Protocol;
using Raido.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class HandshakeHubReconnectTests
{
    [TestMethod]
    public async Task ReconnectWorld_UsesExistingConnectionAndLeavesHandshakeContextOnHandshakeProtocol()
    {
        var authentication = Substitute.For<IAuthenticationService>();
        authentication.AuthenticateWorldReconnectAsync("login", "password")
            .Returns(WorldReconnectAuthenticationResult.Success(42));
        var existing = CreateExistingConnection(masterId: 42, characterMasterId: 42);
        var connections = Substitute.For<IGameConnectionService>();
        connections.FindByMasterId(42).Returns(Task.FromResult<IGameConnection?>(existing));
        var clientProtocol = Substitute.For<IClientProtocol>();
        var resolver = Substitute.For<IClientProtocolResolver>();
        resolver.GetProtocol(742).Returns(clientProtocol);
        var handshakeProtocol = Substitute.For<IRaidoProtocol>();
        var context = CreateContext(handshakeProtocol, handshakeSucceeded: true);
        var hub = CreateHub(authentication, connections, resolver);
        SetClients(hub);
        SetContext(hub, context);

        existing.TryReconnectAsync(
                Arg.Any<RaidoCallerContext>(),
                Arg.Any<IRaidoProtocol>(),
                Arg.Any<Func<ValueTask<bool>>>())
            .Returns(async call => await call.Arg<Func<ValueTask<bool>>>().Invoke());

        await hub.ReconnectWorld(CreateRequest());

        await connections.Received(1).FindByMasterId(42);
        await existing.Received(1).TryReconnectAsync(
            context,
            clientProtocol,
            Arg.Any<Func<ValueTask<bool>>>());
        clientProtocol.Received(1).SetEncryptionSeed(Arg.Any<uint[]>());
        await context.Received(1).WriteHandshakeAsync(
            Arg.Is<WorldReconnectResponse>(response => response.CharacterIndex == 1),
            Arg.Any<CancellationToken>());
        Assert.AreSame(handshakeProtocol, context.Protocol);
        context.DidNotReceive().Abort();
    }

    [TestMethod]
    public async Task ReconnectWorld_RejectsAConnectionWhoseCharacterBelongsToAnotherAccount()
    {
        var authentication = Substitute.For<IAuthenticationService>();
        authentication.AuthenticateWorldReconnectAsync("login", "password")
            .Returns(WorldReconnectAuthenticationResult.Success(42));
        var existing = CreateExistingConnection(masterId: 42, characterMasterId: 99);
        var connections = Substitute.For<IGameConnectionService>();
        connections.FindByMasterId(42).Returns(Task.FromResult<IGameConnection?>(existing));
        var context = CreateContext(Substitute.For<IRaidoProtocol>(), handshakeSucceeded: true);
        var hub = CreateHub(authentication, connections, Substitute.For<IClientProtocolResolver>());
        SetClients(hub);
        SetContext(hub, context);

        await hub.ReconnectWorld(CreateRequest());

        await existing.DidNotReceive().TryReconnectAsync(
            Arg.Any<RaidoCallerContext>(),
            Arg.Any<IRaidoProtocol>(),
            Arg.Any<Func<ValueTask<bool>>>());
        context.Received(1).Abort();
    }

    private static IGameConnection CreateExistingConnection(uint masterId, uint characterMasterId)
    {
        var features = new FeatureCollection();
        features.Set<IAuthenticationFeature>(new AuthenticationFeature
        {
            AuthenticationProperties = new AuthenticationProperties
            {
                Claims = new Dictionary<string, object> { [Claims.Subject] = masterId.ToString() }
            }
        });
        var session = Substitute.For<IGameWorldSession>();
        session.MasterId.Returns(masterId);
        features.Set<Hagalaz.Services.GameWorld.Features.ISessionFeature>(new SessionFeature { Session = session });
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(characterMasterId);
        character.Index.Returns(1);
        character.Location.Returns(new Location(3200, 3200, 0, 0));
        features.Set<ICharacterFeature>(new CharacterFeature { Character = character });

        var connection = Substitute.For<IGameConnection>();
        connection.Features.Returns(features);
        return connection;
    }

    private static WorldReconnectRequest CreateRequest() => new()
    {
        ClientRevision = 742,
        ClientRevisionPatch = 1,
        Login = "login",
        Password = "password",
        IsaacSeed = [1, 2, 3, 4]
    };

    private static RaidoCallerContext CreateContext(IRaidoProtocol protocol, bool handshakeSucceeded)
    {
        var context = Substitute.For<RaidoCallerContext>();
        context.Protocol.Returns(protocol);
        context.ConnectionAbortedToken.Returns(CancellationToken.None);
        context.WriteHandshakeAsync(Arg.Any<WorldReconnectResponse>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(handshakeSucceeded));
        context.Features.Returns(new FeatureCollection());
        return context;
    }

    private static HandshakeHub CreateHub(
        IAuthenticationService authentication,
        IGameConnectionService connections,
        IClientProtocolResolver resolver) => new(
        authentication,
        connections,
        Substitute.For<IClientPermissionProvider>(),
        resolver,
        Substitute.For<ISystemUpdateService>(),
        Options.Create(new ServerConfig { ClientRevision = 742, ClientRevisionPatch = 1 }),
        Options.Create(new WorldOptions()),
        Substitute.For<IConfiguration>(),
        Substitute.For<IScopedGameMediator>(),
        new WorldLifecycleState(),
        new WorldRegistrationStore(),
        new WorldInstanceIdentity());

    private static void SetContext(RaidoHub hub, RaidoCallerContext context) =>
        typeof(RaidoHub)
            .GetProperty(nameof(RaidoHub.Context), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(hub, context);

    private static void SetClients(RaidoHub hub)
    {
        var caller = Substitute.For<IRaidoClientProxy>();
        caller.SendAsync(Arg.Any<ClientSignInResponse>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var clients = Substitute.For<IRaidoCallerClients>();
        clients.Caller.Returns(caller);
        typeof(RaidoHub)
            .GetProperty(nameof(RaidoHub.Clients), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(hub, clients);
    }
}
