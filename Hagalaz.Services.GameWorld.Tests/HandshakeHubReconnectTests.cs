using System.Reflection;
using System.Security.Claims;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Configuration;
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
    public async Task ReconnectWorld_AuthenticatesSendsResponseAndReconnects()
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
        var context = CreateContext(Substitute.For<IRaidoProtocol>());
        var hub = CreateHub(authentication, connections, resolver);
        var caller = SetClients(hub);
        SetContext(hub, context);

        var order = new List<string>();
        caller.SendAsync(Arg.Any<WorldReconnectResponse>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                order.Add("response");
                return Task.CompletedTask;
            });
        existing.TryReconnect(context, clientProtocol)
            .Returns(_ =>
            {
                order.Add("reconnect");
                return true;
            });

        await hub.ReconnectWorld(CreateRequest());

        await connections.Received(1).FindByMasterId(42);
        clientProtocol.Received(1).SetEncryptionSeed(Arg.Is<uint[]>(seed =>
            seed.Length == 4 && seed[0] == 1 && seed[1] == 2 && seed[2] == 3 && seed[3] == 4));
        await caller.Received(1).SendAsync(
            Arg.Is<WorldReconnectResponse>(response =>
                response.CharacterIndex == 1 &&
                response.CharacterLocation.X == 3200 &&
                response.CharacterLocation.Y == 3200),
            Arg.Any<CancellationToken>());
        existing.Received(1).TryReconnect(context, clientProtocol);
        CollectionAssert.AreEqual(new[] { "response", "reconnect" }, order);
        context.DidNotReceive().Abort();
    }

    [TestMethod]
    public async Task ReconnectWorld_RejectsInvalidCredentialsBeforeResolvingConnection()
    {
        var authentication = Substitute.For<IAuthenticationService>();
        authentication.AuthenticateWorldReconnectAsync("login", "password")
            .Returns(WorldReconnectAuthenticationResult.Fail);
        var connections = Substitute.For<IGameConnectionService>();
        var context = CreateContext(Substitute.For<IRaidoProtocol>());
        var hub = CreateHub(authentication, connections, Substitute.For<IClientProtocolResolver>());
        var caller = SetClients(hub);
        SetContext(hub, context);

        await hub.ReconnectWorld(CreateRequest());

        await connections.DidNotReceive().FindByMasterId(Arg.Any<uint>());
        await caller.Received(1).SendAsync(ClientSignInResponse.BadSession, Arg.Any<CancellationToken>());
        context.Received(1).Abort();
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
        var context = CreateContext(Substitute.For<IRaidoProtocol>());
        var hub = CreateHub(authentication, connections, Substitute.For<IClientProtocolResolver>());
        var caller = SetClients(hub);
        SetContext(hub, context);

        await hub.ReconnectWorld(CreateRequest());

        existing.DidNotReceive().TryReconnect(Arg.Any<RaidoCallerContext>(), Arg.Any<IRaidoProtocol>());
        await caller.Received(1).SendAsync(ClientSignInResponse.BadSession, Arg.Any<CancellationToken>());
        context.Received(1).Abort();
    }

    [TestMethod]
    public async Task ReconnectWorld_PropagatesResponseWriteFailure()
    {
        var authentication = Substitute.For<IAuthenticationService>();
        authentication.AuthenticateWorldReconnectAsync("login", "password")
            .Returns(WorldReconnectAuthenticationResult.Success(42));
        var existing = CreateExistingConnection(masterId: 42, characterMasterId: 42);
        var connections = Substitute.For<IGameConnectionService>();
        connections.FindByMasterId(42).Returns(Task.FromResult<IGameConnection?>(existing));
        var resolver = Substitute.For<IClientProtocolResolver>();
        var context = CreateContext(Substitute.For<IRaidoProtocol>());
        var hub = CreateHub(authentication, connections, resolver);
        var caller = SetClients(hub);
        SetContext(hub, context);
        caller.SendAsync(Arg.Any<WorldReconnectResponse>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new IOException("write failed")));

        await Assert.ThrowsExactlyAsync<IOException>(() => hub.ReconnectWorld(CreateRequest()));

        existing.DidNotReceive().TryReconnect(Arg.Any<RaidoCallerContext>(), Arg.Any<IRaidoProtocol>());
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

    private static RaidoCallerContext CreateContext(IRaidoProtocol protocol)
    {
        var context = Substitute.For<RaidoCallerContext>();
        context.Protocol.Returns(protocol);
        context.ConnectionAbortedToken.Returns(CancellationToken.None);
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

    private static IRaidoClientProxy SetClients(RaidoHub hub)
    {
        var caller = Substitute.For<IRaidoClientProxy>();
        caller.SendAsync(Arg.Any<ClientSignInResponse>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        caller.SendAsync(Arg.Any<WorldReconnectResponse>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var clients = Substitute.For<IRaidoCallerClients>();
        clients.Caller.Returns(caller);
        typeof(RaidoHub)
            .GetProperty(nameof(RaidoHub.Clients), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(hub, clients);
        return caller;
    }
}
