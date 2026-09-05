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
        using var metricsMeter = new Meter($"{nameof(WorldReconnectConnectionHandlerTests)}-{Guid.NewGuid()}");
        var connectionHandler = CreateConnectionHandler(metricsMeter);
        var handler = new WorldReconnectConnectionHandler(
            authentication,
            sessions,
            Substitute.For<IGameSessionClaimStore>(),
            new RaidoHubConnectionStore(),
            connectionHandler,
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
                Arg.Any<WorldReconnectAuthenticationRequest>());
            await sessions.DidNotReceive().FindWorldSessionByMasterId(Arg.Any<uint>());
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
        Assert.IsTrue(fixture.Tcp.TryGetCurrentConnection(out var reserved));
        Assert.AreSame(fixture.Replacement, reserved);
        await fixture.ReplacementInput.Writer.WriteAsync(new byte[] { 0x12, 0x34 });

        gate.Release();
        await run;

        Assert.IsTrue(fixture.Tcp.TryGetCurrentConnection(out var current));
        Assert.AreSame(fixture.Replacement, current);
        var buffered = await ReadNonCanceledAsync(fixture.Tcp.Transport.Input);
        CollectionAssert.AreEqual(new byte[] { 0x12, 0x34 }, buffered.Buffer.ToArray());
        fixture.Tcp.Transport.Input.AdvanceTo(buffered.Buffer.End);
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
        var fixture = CreateReconnectFixture(provider, clientProtocol, out var gate);
        var run = fixture.Handler.HandleAsync(
            fixture.Replacement,
            CreateHandshakeProtocol(),
            CreateReconnectRequest(),
            CancellationToken.None);

        await gate.FlushStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
        fixture.Target.Abort();
        gate.Release();
        await run;

        Assert.IsFalse(fixture.Tcp.TryGetCurrentConnection(out _));
        Assert.IsTrue(fixture.Tcp.IsTerminal);
        fixture.Replacement.Received(2).Abort();
        Assert.AreEqual(4611, gate.FlushedBytes!.Length);

        await fixture.DisposeAsync();
    }

    [TestMethod]
    [Timeout(30000)]
    public async Task ConcurrentReconnectCandidates_DoNotReplaceWinnerProtocolOrSendLoserSuccess()
    {
        var initial = CreatePhysicalConnection("stable-logical", out var initialInput, out var initialOutput, out var initialClosed);
        var candidateA = CreatePhysicalConnection("candidate-a", out var inputA, out var outputA, out var closedA);
        var candidateB = CreatePhysicalConnection("candidate-b", out var inputB, out var outputB, out var closedB);
        var candidateC = CreatePhysicalConnection("candidate-c", out var inputC, out var outputC, out var closedC);
        var gateA = new GatedPipeWriter();
        var gateB = new GatedPipeWriter();
        var gateC = new GatedPipeWriter();
        candidateA.Transport.Output.Returns(gateA);
        candidateB.Transport.Output.Returns(gateB);
        candidateC.Transport.Output.Returns(gateC);
        var protocolA = Substitute.For<IClientProtocol>();
        protocolA.Name.Returns("protocol-a");
        protocolA.Version.Returns(742);
        var protocolB = Substitute.For<IClientProtocol>();
        protocolB.Name.Returns("protocol-b");
        protocolB.Version.Returns(742);
        var scopeLifetimes = new List<TrackingScopeLifetime>();
        var resolverCalls = 0;
        await using var provider = new ServiceCollection()
            .AddScoped<TrackingScopeLifetime>()
            .AddScoped<IClientProtocolResolver>(scope =>
            {
                var lifetime = scope.GetRequiredService<TrackingScopeLifetime>();
                lock (scopeLifetimes)
                {
                    scopeLifetimes.Add(lifetime);
                }

                var resolver = Substitute.For<IClientProtocolResolver>();
                resolver.GetProtocol(Arg.Any<int>()).Returns(_ =>
                {
                    var protocol = Interlocked.Increment(ref resolverCalls) == 1 ? protocolA : protocolB;
                    lifetime.ProtocolName = protocol.Name;
                    return protocol;
                });
                return resolver;
            })
            .BuildServiceProvider();
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
        initialClosed.Cancel();
        Assert.IsTrue(tcp.Transport.Input.TryRead(out var boundary));
        tcp.Transport.Input.AdvanceTo(boundary.Buffer.End);
        tcp.AcknowledgeInputBoundary();

        var session = Substitute.For<IGameWorldSession>();
        session.ConnectionId.Returns("stable-logical");
        session.MasterId.Returns(42u);
        session.SessionClaimId.Returns("claim");
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        character.Session.Returns(session);
        character.Index.Returns(1);
        character.Location.Returns(Location.Create(3200, 3200));
        character.GameClient.Returns(new GameClient(DisplayMode.FixedScreen, Language.English, 800, 600));
        target.Features.Set<Hagalaz.Services.GameWorld.Features.ISessionFeature>(new SessionFeature { Session = session });
        target.Features.Set<ICharacterFeature>(new CharacterFeature { Character = character });
        target.Features.Set<IAuthenticationFeature>(new AuthenticationFeature
        {
            AuthenticationProperties = new AuthenticationProperties
            {
                Claims = new Dictionary<string, object> { [OpenIddictConstants.Claims.Subject] = "42" }
            }
        });

        var connections = new RaidoHubConnectionStore();
        connections.Add(target);
        using var meter = new Meter($"{nameof(WorldReconnectConnectionHandlerTests)}-{Guid.NewGuid()}");
        var connectionHandler = CreateConnectionHandler(meter);
        var authentication = Substitute.For<IAuthenticationService>();
#pragma warning disable CA2012 // NSubstitute consumes the configured ValueTask exactly once.
        authentication.AuthenticateWorldReconnectAsync(Arg.Any<WorldReconnectAuthenticationRequest>())
            .Returns(new ValueTask<WorldReconnectAuthenticationResult>(WorldReconnectAuthenticationResult.Success(42)));
#pragma warning restore CA2012
        var sessions = Substitute.For<IGameSessionService>();
        sessions.FindWorldSessionByMasterId(42).Returns(Task.FromResult<IGameWorldSession?>(session));
        var claims = Substitute.For<IGameSessionClaimStore>();
        claims.ExecuteIfOwnerAsync(
                42,
                "claim",
                Arg.Any<Func<CancellationToken, Task<bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<CancellationToken, Task<bool>>>()!(CancellationToken.None));
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

        try
        {
            var taskA = handler.HandleAsync(candidateA, CreateHandshakeProtocol(), CreateReconnectRequest(1), CancellationToken.None);
            await gateA.FlushStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            var taskB = handler.HandleAsync(candidateB, CreateHandshakeProtocol(), CreateReconnectRequest(5), CancellationToken.None);
            await taskB.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.IsNull(gateB.FlushedBytes);

            gateA.Release();
            await taskA.WaitAsync(TimeSpan.FromSeconds(1));

            Assert.IsTrue(tcp.TryGetCurrentConnection(out var current));
            Assert.AreSame(candidateA, current);
            Assert.AreSame(protocolA, target.Protocol);

            candidateB.Received(1).Abort();

            var taskC = handler.HandleAsync(candidateC, CreateHandshakeProtocol(), CreateReconnectRequest(9), CancellationToken.None);
            await taskC.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.IsNull(gateC.FlushedBytes);
            candidateC.Received(1).Abort();
            Assert.AreSame(candidateA, current);
            Assert.AreSame(protocolA, target.Protocol);

            TrackingScopeLifetime winnerLifetime;
            TrackingScopeLifetime loserLifetimeB;
            TrackingScopeLifetime loserLifetimeC;
            lock (scopeLifetimes)
            {
                winnerLifetime = scopeLifetimes.Single(lifetime => lifetime.ProtocolName == "protocol-a");
                var loserLifetimes = scopeLifetimes.Where(lifetime => lifetime.ProtocolName == "protocol-b").ToArray();
                Assert.AreEqual(2, loserLifetimes.Length);
                loserLifetimeB = loserLifetimes[0];
                loserLifetimeC = loserLifetimes[1];
            }

            Assert.AreEqual(0, winnerLifetime.DisposeCount);
            Assert.AreEqual(1, loserLifetimeB.DisposeCount);
            Assert.AreEqual(1, loserLifetimeC.DisposeCount);

            await target.CleanupAsync();
            Assert.AreEqual(1, winnerLifetime.DisposeCount);
        }
        finally
        {
            target.Abort();
            await target.CleanupAsync();
            closedA.Dispose();
            closedB.Dispose();
            closedC.Dispose();
            await inputA.Reader.CompleteAsync();
            await inputA.Writer.CompleteAsync();
            await outputA.Reader.CompleteAsync();
            await outputA.Writer.CompleteAsync();
            await inputB.Reader.CompleteAsync();
            await inputB.Writer.CompleteAsync();
            await outputB.Reader.CompleteAsync();
            await outputB.Writer.CompleteAsync();
            await inputC.Reader.CompleteAsync();
            await inputC.Writer.CompleteAsync();
            await outputC.Reader.CompleteAsync();
            await outputC.Writer.CompleteAsync();
            await initialInput.Reader.CompleteAsync();
            await initialInput.Writer.CompleteAsync();
            await initialOutput.Reader.CompleteAsync();
            await initialOutput.Writer.CompleteAsync();
        }
    }

    private static HandshakeProtocol CreateHandshakeProtocol() =>
        new(new TestHandshakeCodec(), Options.Create(new Hagalaz.Services.GameWorld.Configuration.Model.ServerConfig
        { ClientRevision = 742 }));

    private static RaidoHubConnectionHandler CreateConnectionHandler(Meter meter)
    {
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        return new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoHubLifetimeManager>(),
            Substitute.For<IRaidoDispatcher>(),
            new RaidoMetrics(meterFactory));
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

    private static WorldReconnectRequest CreateReconnectRequest(uint seed = 1) => new()
    {
        ClientRevision = 742,
        Login = "login",
        Password = "password",
        IsaacSeed = new uint[] { seed, seed + 1, seed + 2, seed + 3 }
    };

    private static ReconnectFixture CreateReconnectFixture(
        ServiceProvider provider,
        IClientProtocol clientProtocol,
        out GatedPipeWriter gate)
    {
        var stableConnectionId = "stable-logical";
        var initial = CreatePhysicalConnection(stableConnectionId, out var initialInput, out var initialOutput, out var initialClosed);
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
        initialClosed.Cancel();
        Assert.IsTrue(tcp.Transport.Input.TryRead(out var boundary));
        Assert.IsTrue(boundary.IsCanceled);
        Assert.IsTrue(boundary.Buffer.IsEmpty);
        tcp.Transport.Input.AdvanceTo(boundary.Buffer.End);
        tcp.AcknowledgeInputBoundary();

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
        var metricsMeter = new Meter($"{nameof(WorldReconnectConnectionHandlerTests)}-{Guid.NewGuid()}");
        var connectionHandler = CreateConnectionHandler(metricsMeter);
        var authentication = Substitute.For<IAuthenticationService>();
#pragma warning disable CA2012 // NSubstitute consumes the configured ValueTask exactly once.
        authentication.AuthenticateWorldReconnectAsync(Arg.Any<WorldReconnectAuthenticationRequest>())
            .Returns(new ValueTask<WorldReconnectAuthenticationResult>(WorldReconnectAuthenticationResult.Success(42)));
#pragma warning restore CA2012
        var sessions = Substitute.For<IGameSessionService>();
        sessions.FindWorldSessionByMasterId(42).Returns(Task.FromResult<IGameWorldSession?>(session));
        var claims = Substitute.For<IGameSessionClaimStore>();
        claims.ExecuteIfOwnerAsync(
                42,
                "claim",
                Arg.Any<Func<CancellationToken, Task<bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<CancellationToken, Task<bool>>>()!(CancellationToken.None));
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
            metricsMeter);
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

    private static async Task<ReadResult> ReadNonCanceledAsync(PipeReader reader)
    {
        while (true)
        {
            var result = await reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));
            if (!result.IsCanceled || !result.Buffer.IsEmpty)
            {
                return result;
            }

            reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
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

    private sealed class TrackingScopeLifetime : IAsyncDisposable
    {
        private int _disposeCount;

        public string? ProtocolName { get; set; }

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            return ValueTask.CompletedTask;
        }
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

}
