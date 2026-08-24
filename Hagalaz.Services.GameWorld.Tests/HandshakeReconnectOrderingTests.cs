using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Security.Claims;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Hubs;
using Hagalaz.Services.GameWorld.Model;
using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Hagalaz.Services.GameWorld.Network.Model;
using Hagalaz.Services.GameWorld.Network.Protocol;
using Hagalaz.Services.GameWorld.Providers;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using OpenIddict.Abstractions;
using Raido.Common.Protocol;
using Raido.Server;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class HandshakeReconnectOrderingTests
{
    [TestMethod]
    [Timeout(5000)]
    public async Task ReconnectWorldCommitsBeforeResyncAndFlushesResponseFirst()
    {
        using var original = new TestPhysicalConnection("original");
        using var replacementPhysical = new TestPhysicalConnection("replacement");
        var handshakeProtocol = new RecordingProtocol(message => message is WorldSignInResponse ? (byte)1 : (byte)9);
        var clientProtocol = new RecordingClientProtocol(message =>
            message switch
            {
                DrawDynamicMapMessage => 2,
                ReconnectGameTraffic => 4,
                _ => 3
            });
        var target = CreateContext(original, handshakeProtocol);
        var replacement = CreateContext(replacementPhysical, handshakeProtocol);
        var store = new RaidoConnectionStore();
        store.Add(target);
        store.Add(replacement);

        var proxy = new ContextClientProxy(target);
        var worldSession = new WorldGameSession(17, target.ConnectionId, proxy, "session-1");
        var character = Substitute.For<ICharacter>();
        var appearance = Substitute.For<ICharacterAppearance>();
        character.MasterId.Returns((uint)17);
        character.DisplayName.Returns("Tester");
        character.Index.Returns(42);
        character.Session.Returns(worldSession);
        character.Appearance.Returns(appearance);
        character.WhenForAnyArgs(value => value.UpdateMap(default, default)).Do(_ => worldSession.SendMessage(new DrawDynamicMapMessage()));
        appearance.When(value => value.Refresh()).Do(_ => worldSession.SendMessage(new AppearanceRefreshMessage()));
        target.Features.Set<Hagalaz.Services.GameWorld.Features.ISessionFeature>(new SessionFeature { Session = worldSession });
        target.Features.Set<ICharacterFeature>(new CharacterFeature { Character = character });
        SetAuthentication(target, 17);
        target.Features.Get<IRaidoStatefulReconnectFeature>()!.EnableReconnect();
        target.Features.Get<IRaidoStatefulReconnectFeature>()!.OnReconnected(_ =>
        {
            worldSession.SendMessage(new ReconnectGameTraffic());
            return Task.CompletedTask;
        });

        original.Closed.Cancel();
        await WaitUntilAsync(() => target.LifecycleState == RaidoConnectionLifecycleState.Reconnecting);

        SetAuthentication(replacement, 17);
        var authentication = Substitute.For<IAuthenticationService>();
        authentication.AuthenticateWorldReconnectAsync(Arg.Any<SignInRequest>()).Returns(new ValueTask<SignInResult>(SignInResult.Success));
        var protocolResolver = Substitute.For<IClientProtocolResolver>();
        protocolResolver.GetProtocol(742).Returns(clientProtocol);
        var sessions = Substitute.For<IGameSessionService>();
        sessions.FindByMasterId(17).Returns(worldSession);
        var lifecycle = new WorldLifecycleState();
        lifecycle.MarkApplicationStarted();
        lifecycle.MarkStarted();
        lifecycle.MarkCompleted();
        lifecycle.MarkRegistrationSucceeded();
        var identity = new WorldInstanceIdentity();
        var registrations = new WorldRegistrationStore();
        registrations.ObserveOnline(new Hagalaz.Game.Messages.WorldOnlineMessage
        {
            Id = 1,
            Name = "World",
            IpAddress = "127.0.0.1",
            Port = 43594,
            CharacterCount = 1,
            InstanceId = identity.InstanceId,
            Generation = identity.Generation,
            StartedAt = identity.StartedAt,
            LastSeenAt = DateTimeOffset.UtcNow,
            LeaseExpiresAt = DateTimeOffset.UtcNow.AddMinutes(1),
            Settings = new Hagalaz.Game.Messages.WorldOnlineMessage.WorldSettings
            {
                IsMembersOnly = true,
                IsQuickChatEnabled = false,
                IsPvP = false,
                IsLootShareEnabled = false,
                IsHighLighted = false
            },
            Location = new Hagalaz.Game.Messages.WorldOnlineMessage.WorldLocation { Name = "Test", Flag = 0 }
        });

        using var hub = new HandshakeHub(
            authentication,
            Substitute.For<IClientPermissionProvider>(),
            protocolResolver,
            Substitute.For<Hagalaz.Game.Abstractions.Services.ISystemUpdateService>(),
            Options.Create(new ServerConfig { ClientRevision = 742, ClientRevisionPatch = 1 }),
            Options.Create(new WorldOptions { Id = 1 }),
            new ConfigurationBuilder().Build(),
            Substitute.For<IScopedGameMediator>(),
            lifecycle,
            registrations,
            identity,
            sessions,
            store);
        hub.Context = replacement.RaidoCallerContext;
        hub.Clients = Substitute.For<IRaidoCallerClients>();

        await hub.ReconnectWorld(new WorldReconnectRequest
        {
            ClientRevision = 742,
            ClientRevisionPatch = 1,
            Login = "login",
            Password = "password",
            IsaacSeed = new uint[] { 1, 2, 3, 4 }
        });

        var transfer = replacement.TakePendingTransfer();
        Assert.IsNotNull(transfer);
        Assert.IsTrue(await transfer!.CommitAsync(() => new ValueTask<ReadOnlyMemory<byte>>(ReadOnlyMemory<byte>.Empty)));
        replacement.CompleteTransferred();

        var result = await ReadOutputAsync(replacementPhysical.Output.Reader, 4);
        Assert.AreEqual("1,2,3,4", string.Join(',', result));
        Assert.AreSame(clientProtocol, target.Protocol);
        character.Received(1).UpdateMap(true, true);
        appearance.Received(1).Refresh();
        target.Abort();
    }

    private static RaidoConnectionContext CreateContext(TestPhysicalConnection physical, IRaidoProtocol protocol)
    {
        var context = new RaidoConnectionContext(physical.Context, new RaidoConnectionContextOptions
        {
            StatefulReconnectEnabled = true,
            StatefulReconnectGracePeriod = TimeSpan.FromSeconds(1)
        }, NullLoggerFactory.Instance)
        {
            Protocol = protocol
        };
        _ = context.StartPhysicalSession();
        return context;
    }

    private static AuthenticationFeature CreateAuthenticationFeature(uint masterId) => new()
    {
        AuthenticationProperties = new AuthenticationProperties
        {
            Claims = new Dictionary<string, object> { [OpenIddictConstants.Claims.Subject] = masterId.ToString() }
        },
        User = new ClaimsPrincipal(new ClaimsIdentity([], ClaimTypes.Authentication, ClaimTypes.Name, ClaimTypes.Role))
    };

    private static void SetAuthentication(RaidoConnectionContext context, uint masterId)
    {
        var feature = CreateAuthenticationFeature(masterId);
        context.Features.Set<IAuthenticationFeature>(feature);
        context.Features.Set<Microsoft.AspNetCore.Connections.Features.IConnectionUserFeature>(feature);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++) await Task.Delay(1);
        Assert.IsTrue(condition());
    }

    private static async Task<byte[]> ReadOutputAsync(PipeReader reader, int expectedLength)
    {
        var bytes = new List<byte>(expectedLength);
        while (bytes.Count < expectedLength)
        {
            var result = await reader.ReadAsync();
            bytes.AddRange(result.Buffer.ToArray());
            reader.AdvanceTo(result.Buffer.End);
            if (result.IsCompleted) break;
        }

        return bytes.ToArray();
    }

    private sealed class ReconnectGameTraffic : RaidoMessage;
    private sealed class AppearanceRefreshMessage : RaidoMessage;

    private sealed class ContextClientProxy(RaidoConnectionContext context) : IRaidoClientProxy
    {
        public Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : RaidoMessage =>
            context.WriteAsync(message, cancellationToken).AsTask();
    }

    private class RecordingProtocol(Func<RaidoMessage, byte> encode) : IRaidoProtocol
    {
        public string Name => "test";
        public int Version => 742;
        public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out RaidoMessage? message)
        {
            if (input.IsEmpty)
            {
                consumed = input.Start;
                examined = input.End;
                message = null;
                return false;
            }

            consumed = input.GetPosition(1);
            examined = input.End;
            message = new ReconnectGameTraffic();
            return true;
        }
        public void WriteMessage(RaidoMessage message, IBufferWriter<byte> output)
        {
            output.GetSpan(1)[0] = encode(message);
            output.Advance(1);
        }
        public ReadOnlyMemory<byte> GetMessageBytes(RaidoMessage message) => new[] { encode(message) };
        public bool IsVersionSupported(int version) => version == Version;
    }

    private sealed class RecordingClientProtocol(Func<RaidoMessage, byte> encode) : RecordingProtocol(encode), IClientProtocol
    {
        public void SetEncryptionSeed(uint[] keys) { }
    }

    private sealed class TestPhysicalConnection : IDisposable
    {
        public readonly CancellationTokenSource Closed = new();
        public readonly Pipe Input = new();
        public readonly Pipe Output = new();
        public readonly ConnectionContext Context;

        public TestPhysicalConnection(string id)
        {
            var transport = Substitute.For<IDuplexPipe>();
            transport.Input.Returns(Input.Reader);
            transport.Output.Returns(Output.Writer);
            Context = Substitute.For<ConnectionContext>();
            Context.ConnectionId.Returns(id);
            Context.Transport.Returns(transport);
            Context.Features.Returns(new FeatureCollection());
            Context.Items.Returns(new Dictionary<object, object?>());
            Context.ConnectionClosed.Returns(Closed.Token);
        }

        public void Dispose()
        {
            Closed.Cancel();
            Input.Reader.Complete();
            Output.Reader.Complete();
            Closed.Dispose();
        }
    }
}
