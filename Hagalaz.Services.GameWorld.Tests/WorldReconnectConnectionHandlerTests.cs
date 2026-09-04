using System.Buffers;
using System.Diagnostics.Metrics;
using System.IO.Pipelines;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Model;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Network;
using Hagalaz.Services.GameWorld.Network.Handshake;
using Hagalaz.Services.GameWorld.Network.Handshake.Encoders;
using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Hagalaz.Services.GameWorld.Network.Protocol;
using Hagalaz.Services.GameWorld.Providers;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using OpenIddict.Abstractions;
using Raido.Common.Messages;
using Raido.Common.Protocol;
using Raido.Server;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class WorldReconnectConnectionHandlerTests
{
    [TestMethod]
    public async Task HandleAsync_WhenInjectedValidatorRejects_DoesNotAuthenticate()
    {
        var validator = Substitute.For<IHandshakeValidator<WorldReconnectRequest>>();
        validator.Validate(Arg.Any<WorldReconnectRequest>()).Returns(ClientSignInResponse.Outdated);
        var authentication = Substitute.For<IAuthenticationService>();
        var sessions = Substitute.For<IGameSessionService>();
        var connection = CreateConnection(out var input, out var output);
        await using var provider = new ServiceCollection().BuildServiceProvider();
        using var infrastructure = CreateConnectionHandlerSubstitute();
        var handler = new WorldReconnectConnectionHandler(
            authentication,
            sessions,
            Substitute.For<IGameSessionClaimStore>(),
            new RaidoHubConnectionStore(),
            infrastructure.Handler,
            provider.GetRequiredService<IServiceScopeFactory>(),
            validator,
            NullLogger<WorldReconnectConnectionHandler>.Instance);

        try
        {
            await handler.HandleAsync(
                connection,
                CreateHandshakeProtocol(),
                new WorldReconnectRequest(),
                CancellationToken.None);

            await authentication.DidNotReceive().AuthenticateWorldReconnectAsync(
                Arg.Any<string>(),
                Arg.Any<string>());
            await sessions.DidNotReceive().FindWorldSessionByMasterId(Arg.Any<uint>());
            infrastructure.Handler.DidNotReceiveWithAnyArgs().TryActivatePhysicalConnection(null!, null!);
            Assert.AreEqual((byte)6, await ReadByteAsync(output.Reader));
        }
        finally
        {
            connection.Abort();
            await input.Reader.CompleteAsync();
            await input.Writer.CompleteAsync();
            await CompleteConnectionAsync(output);
        }
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task HandleAsync_FlushesResponseBeforeAttachAndLeavesImmediateInputBuffered()
    {
        var clientProtocol = Substitute.For<IClientProtocol>();
        clientProtocol.Name.Returns("test-client");
        clientProtocol.Version.Returns(742);
        var resolver = Substitute.For<IClientProtocolResolver>();
        resolver.GetProtocol(Arg.Any<int>()).Returns(clientProtocol);
        await using var provider = new ServiceCollection()
            .AddScoped<IClientProtocolResolver>(_ => resolver)
            .BuildServiceProvider();
        var fixture = CreateReconnectFixture(provider, clientProtocol, out var gate);
        var handler = fixture.Handler;
        var reconnect = CreateReconnectRequest();

        var run = handler.HandleAsync(fixture.Replacement, CreateHandshakeProtocol(), reconnect, CancellationToken.None);
        await gate.FlushStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.AreSame(fixture.ClientProtocol, fixture.Target.Protocol);
        Assert.IsFalse(fixture.Tcp.TryGetCurrentConnection(out _));
        fixture.ConnectionHandler.DidNotReceiveWithAnyArgs().TryActivatePhysicalConnection(null!, null!);
        await fixture.ReplacementInput.Writer.WriteAsync(new byte[] { 0x12, 0x34 });
        Assert.IsTrue(fixture.ReplacementInput.Reader.TryRead(out var buffered));
        CollectionAssert.AreEqual(new byte[] { 0x12, 0x34 }, buffered.Buffer.ToArray());
        fixture.ReplacementInput.Reader.AdvanceTo(buffered.Buffer.Start, buffered.Buffer.End);

        gate.Release();
        await run;

        fixture.ConnectionHandler.Received(1).TryActivatePhysicalConnection(fixture.Target, fixture.Replacement);
        Assert.IsFalse(fixture.Tcp.TryGetCurrentConnection(out _));
        Assert.AreEqual(fixture.StableConnectionId, fixture.Target.ConnectionId);
        fixture.ClientProtocol.Received(1).SetEncryptionSeed(
            Arg.Is<uint[]>(seed => seed.SequenceEqual(new uint[] { 1, 2, 3, 4 })));
        Assert.AreEqual(4611, gate.FlushedBytes!.Length);
        Assert.AreEqual(15, gate.FlushedBytes[0]);
        Assert.AreEqual(0x12, gate.FlushedBytes[1]);
        Assert.AreEqual(0, gate.FlushedBytes[2]);

        await fixture.DisposeAsync();
    }

    [TestMethod]
    [Timeout(5000)]
    public async Task HandleAsync_WhenInfrastructureRejectsAfterResponse_RejectsActivation()
    {
        var clientProtocol = Substitute.For<IClientProtocol>();
        clientProtocol.Name.Returns("test-client");
        clientProtocol.Version.Returns(742);
        var resolver = Substitute.For<IClientProtocolResolver>();
        resolver.GetProtocol(Arg.Any<int>()).Returns(clientProtocol);
        await using var provider = new ServiceCollection()
            .AddScoped<IClientProtocolResolver>(_ => resolver)
            .BuildServiceProvider();
        var fixture = CreateReconnectFixture(provider, clientProtocol, out var gate, activationAccepted: false);
        var run = fixture.Handler.HandleAsync(
            fixture.Replacement,
            CreateHandshakeProtocol(),
            CreateReconnectRequest(),
            CancellationToken.None);

        await gate.FlushStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
        gate.Release();
        await run;

        fixture.ConnectionHandler.Received(1).TryActivatePhysicalConnection(fixture.Target, fixture.Replacement);
        Assert.IsFalse(fixture.Tcp.TryGetCurrentConnection(out _));
        Assert.IsFalse(fixture.Tcp.IsTerminal);
        fixture.Replacement.Received(1).Abort();
        Assert.AreEqual(4611, gate.FlushedBytes!.Length);

        await fixture.DisposeAsync();
    }

    private static HandshakeProtocol CreateHandshakeProtocol() =>
        new(new TestHandshakeCodec(), Options.Create(new Hagalaz.Services.GameWorld.Configuration.Model.ServerConfig
        { ClientRevision = 742 }));

    private static ConnectionHandlerSubstitute CreateConnectionHandlerSubstitute()
    {
        var meter = new Meter($"{nameof(WorldReconnectConnectionHandlerTests)}-{Guid.NewGuid()}");
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        var metrics = new RaidoMetrics(meterFactory);
        var handler = Substitute.For<RaidoHubConnectionHandler>(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoHubLifetimeManager>(),
            Substitute.For<IRaidoDispatcher>(),
            metrics);
        return new ConnectionHandlerSubstitute(handler, meter);
    }

    private static ConnectionContext CreateConnection(out Pipe input, out Pipe output)
    {
        input = new Pipe();
        output = new Pipe();
        var transport = Substitute.For<IDuplexPipe>();
        transport.Input.Returns(input.Reader);
        transport.Output.Returns(output.Writer);
        var connection = Substitute.For<ConnectionContext>();
        connection.Transport.Returns(transport);
        connection.ConnectionClosed.Returns(CancellationToken.None);
        return connection;
    }

    private static WorldReconnectRequest CreateReconnectRequest() => new()
    {
        ClientRevision = 742,
        Login = "login",
        Password = "password",
        IsaacSeed = new uint[] { 1, 2, 3, 4 }
    };

    private static ReconnectFixture CreateReconnectFixture(
        ServiceProvider provider,
        IClientProtocol clientProtocol,
        out GatedPipeWriter gate,
        bool activationAccepted = true)
    {
        var stableConnectionId = "stable-logical";
        var initial = CreatePhysicalConnection(stableConnectionId, out var initialInput, out var initialOutput, out _);
        var replacement = CreatePhysicalConnection("replacement", out var replacementInput, out var replacementOutput, out var replacementClosed);
        gate = new GatedPipeWriter();
        replacement.Transport.Output.Returns(gate);

        var options = new RaidoConnectionContextOptions
        {
            StatefulReconnectEnabled = true,
            StatefulReconnectTimeout = TimeSpan.FromSeconds(5)
        };
        var tcp = new RaidoTcpConnectionContext(options, NullLoggerFactory.Instance);
        Assert.IsTrue(tcp.TryAttachPhysicalConnection(initial));
        var target = new RaidoHubConnectionContext(
            tcp,
            options,
            Substitute.For<IRaidoProtocol>(),
            NullLoggerFactory.Instance,
            TimeProvider.System);
        tcp.OnPhysicalConnectionClosed(initial);

        var session = Substitute.For<IGameWorldSession>();
        session.ConnectionId.Returns(stableConnectionId);
        session.MasterId.Returns(42u);
        session.SessionClaimId.Returns("claim");
        var gameClient = new GameClient(DisplayMode.FixedScreen, Language.English, 800, 600);
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        character.Session.Returns(session);
        character.Index.Returns(1);
        character.Location.Returns(Location.Create(3200, 3200));
        character.GameClient.Returns(gameClient);
        target.Features.Set<Hagalaz.Services.GameWorld.Features.ISessionFeature>(new SessionFeature { Session = session });
        target.Features.Set<ICharacterFeature>(new CharacterFeature { Character = character });
        target.Features.Set<IAuthenticationFeature>(new AuthenticationFeature
        {
            AuthenticationProperties = new AuthenticationProperties
            {
                Claims = new Dictionary<string, object>
                {
                    [OpenIddictConstants.Claims.Subject] = "42"
                }
            }
        });

        var connections = new RaidoHubConnectionStore();
        connections.Add(target);
        var infrastructure = CreateConnectionHandlerSubstitute();
        var connectionHandler = infrastructure.Handler;
        connectionHandler.TryActivatePhysicalConnection(target, replacement).Returns(activationAccepted);
        var authentication = Substitute.For<IAuthenticationService>();
        authentication.AuthenticateWorldReconnectAsync("login", "password")
            .Returns(new ValueTask<WorldReconnectAuthenticationResult>(WorldReconnectAuthenticationResult.Success(42)));
        var sessions = Substitute.For<IGameSessionService>();
        sessions.FindWorldSessionByMasterId(42).Returns(Task.FromResult<IGameWorldSession?>(session));
        var claims = Substitute.For<IGameSessionClaimStore>();
        claims.ExecuteIfOwnerAsync(
                42,
                "claim",
                Arg.Any<Func<CancellationToken, Task<bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<CancellationToken, Task<bool>>>()(CancellationToken.None));
        var validator = Substitute.For<IHandshakeValidator<WorldReconnectRequest>>();
        validator.Validate(Arg.Any<WorldReconnectRequest>()).Returns(ClientSignInResponse.Success);
        var handler = new WorldReconnectConnectionHandler(
            authentication,
            sessions,
            claims,
            connections,
            connectionHandler,
            provider.GetRequiredService<IServiceScopeFactory>(),
            validator,
            NullLogger<WorldReconnectConnectionHandler>.Instance);

        return new ReconnectFixture(
            handler,
            target,
            tcp,
            connectionHandler,
            replacement,
            replacementInput,
            replacementOutput,
            initialInput,
            initialOutput,
            replacementClosed,
            stableConnectionId,
            clientProtocol,
            gate,
            infrastructure.Meter);
    }

    private static ConnectionContext CreatePhysicalConnection(
        string connectionId,
        out Pipe input,
        out Pipe output,
        out CancellationTokenSource closed)
    {
        input = new Pipe();
        output = new Pipe();
        closed = new CancellationTokenSource();
        var transport = Substitute.For<IDuplexPipe>();
        transport.Input.Returns(input.Reader);
        transport.Output.Returns(output.Writer);
        var connection = Substitute.For<ConnectionContext>();
        connection.ConnectionId.Returns(connectionId);
        connection.Features.Returns(new FeatureCollection());
        connection.Transport.Returns(transport);
        connection.ConnectionClosed.Returns(closed.Token);
        return connection;
    }

    private static async Task<byte> ReadByteAsync(PipeReader reader)
    {
        var result = await reader.ReadAsync();
        try
        {
            Assert.IsFalse(result.Buffer.IsEmpty);
            return result.Buffer.FirstSpan[0];
        }
        finally
        {
            reader.AdvanceTo(result.Buffer.End);
        }
    }

    private static async Task CompleteConnectionAsync(Pipe output)
    {
        await output.Reader.CompleteAsync();
        await output.Writer.CompleteAsync();
    }

    private sealed class TestHandshakeCodec : IRaidoCodec<HandshakeProtocol>
    {
        private readonly WorldReconnectResponseEncoder _reconnectEncoder =
            new(new CharacterLocationService());

        public bool TryDecodeMessage(int opcode, in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            message = null;
            return false;
        }

        public bool TryEncodeMessage<TMessage>(TMessage message, IRaidoMessageBinaryWriter output)
            where TMessage : RaidoMessage
        {
            if (message is ClientSignInResponse response)
            {
                output.SetOpcode(response.GetOpcode());
                return true;
            }

            if (message is WorldReconnectResponse reconnectResponse)
            {
                _reconnectEncoder.EncodeMessage(reconnectResponse, output);
                return true;
            }

            return false;
        }
    }

    private sealed class GatedPipeWriter : PipeWriter
    {
        private readonly ArrayBufferWriter<byte> _buffer = new();
        private readonly TaskCompletionSource<FlushResult> _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource FlushStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public byte[]? FlushedBytes { get; private set; }

        public void Release() => _release.TrySetResult(new FlushResult(false, false));

        public override void Advance(int bytes) => _buffer.Advance(bytes);

        public override Memory<byte> GetMemory(int sizeHint = 0) => _buffer.GetMemory(sizeHint);

        public override Span<byte> GetSpan(int sizeHint = 0) => _buffer.GetSpan(sizeHint);

        public override void CancelPendingFlush() => Release();

        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
        {
            FlushedBytes = _buffer.WrittenSpan.ToArray();
            FlushStarted.TrySetResult();
            return new ValueTask<FlushResult>(_release.Task);
        }

        public override void Complete(Exception? exception = null) { }
    }

    private sealed class ReconnectFixture : IAsyncDisposable
    {
        private readonly Meter _metricsMeter;

        public ReconnectFixture(
            WorldReconnectConnectionHandler handler,
            RaidoHubConnectionContext target,
            RaidoTcpConnectionContext tcp,
            RaidoHubConnectionHandler connectionHandler,
            ConnectionContext replacement,
            Pipe replacementInput,
            Pipe replacementOutput,
            Pipe initialInput,
            Pipe initialOutput,
            CancellationTokenSource replacementClosed,
            string stableConnectionId,
            IClientProtocol clientProtocol,
            GatedPipeWriter gate,
            Meter metricsMeter)
        {
            Handler = handler;
            Target = target;
            Tcp = tcp;
            ConnectionHandler = connectionHandler;
            Replacement = replacement;
            ReplacementInput = replacementInput;
            ReplacementOutput = replacementOutput;
            InitialInput = initialInput;
            InitialOutput = initialOutput;
            ReplacementClosed = replacementClosed;
            StableConnectionId = stableConnectionId;
            ClientProtocol = clientProtocol;
            Gate = gate;
            _metricsMeter = metricsMeter;
        }

        public WorldReconnectConnectionHandler Handler { get; }
        public RaidoHubConnectionContext Target { get; }
        public RaidoTcpConnectionContext Tcp { get; }
        public RaidoHubConnectionHandler ConnectionHandler { get; }
        public ConnectionContext Replacement { get; }
        public Pipe ReplacementInput { get; }
        public Pipe ReplacementOutput { get; }
        public Pipe InitialInput { get; }
        public Pipe InitialOutput { get; }
        public CancellationTokenSource ReplacementClosed { get; }
        public string StableConnectionId { get; }
        public IClientProtocol ClientProtocol { get; }
        public GatedPipeWriter Gate { get; }

        public async ValueTask DisposeAsync()
        {
            Target.Abort();
            await Target.CleanupAsync();
            ReplacementClosed.Dispose();
            await ReplacementInput.Reader.CompleteAsync();
            await ReplacementInput.Writer.CompleteAsync();
            await ReplacementOutput.Reader.CompleteAsync();
            await ReplacementOutput.Writer.CompleteAsync();
            await InitialInput.Reader.CompleteAsync();
            await InitialInput.Writer.CompleteAsync();
            await InitialOutput.Reader.CompleteAsync();
            await InitialOutput.Writer.CompleteAsync();
            _metricsMeter.Dispose();
        }
    }

    private sealed class ConnectionHandlerSubstitute : IDisposable
    {
        private readonly Meter _meter;

        public ConnectionHandlerSubstitute(RaidoHubConnectionHandler handler, Meter meter)
        {
            Handler = handler;
            Meter = meter;
            _meter = meter;
        }

        public RaidoHubConnectionHandler Handler { get; }
        public Meter Meter { get; }

        public void Dispose() => _meter.Dispose();
    }
}
