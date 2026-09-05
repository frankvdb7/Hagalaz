using System.Buffers;
using System.Diagnostics.Metrics;
using System.IO.Pipelines;
using System.Net;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Model;
using Hagalaz.Services.GameWorld.Network;
using Hagalaz.Services.GameWorld.Network.Handshake;
using Hagalaz.Services.GameWorld.Network.Handshake.Encoders;
using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Hagalaz.Services.GameWorld.Network.Protocol;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using Hagalaz.Services.GameWorld.Providers;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
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
public sealed class ClientConnectionHandlerTests
{
    [TestMethod]
    public async Task Bootstrap_Opcode14ThenFreshOpcode16_CreatesStatefulLogicalConnectionAndRetainsAuthentication()
    {
        var fixture = CreateFixture();
        try
        {
            await fixture.Input.Writer.WriteAsync(new byte[] { 14, 16, 0 });

            await fixture.Handler.OnConnectedAsync(fixture.Connection);

            Assert.IsTrue(fixture.CreatedStatefulReconnect);
            CollectionAssert.AreEqual(new byte[] { 0 }, await ReadAllAsync(fixture.Output.Reader));
            Assert.IsInstanceOfType(fixture.DispatchedMessage, typeof(WorldSignInRequest));
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task Bootstrap_Opcode14ThenReconnectOpcode16_InvokesReconnectWithoutCreatingLogicalConnection()
    {
        var fixture = CreateFixture();
        try
        {
            await fixture.Input.Writer.WriteAsync(new byte[] { 14, 16, 1 });

            await fixture.Handler.OnConnectedAsync(fixture.Connection);

            Assert.IsFalse(fixture.FactoryCalled);
            Assert.IsNotNull(fixture.ValidatedReconnectMessage);
            CollectionAssert.AreEqual(new byte[] { 0, 6 }, await ReadAllAsync(fixture.Output.Reader));
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task Bootstrap_Opcode14ThenLobbyOpcode19_CreatesNonStatefulLogicalConnection()
    {
        var fixture = CreateFixture();
        try
        {
            await fixture.Input.Writer.WriteAsync(new byte[] { 14, 19 });

            await fixture.Handler.OnConnectedAsync(fixture.Connection);

            Assert.IsFalse(fixture.CreatedStatefulReconnect);
            Assert.IsInstanceOfType(fixture.DispatchedMessage, typeof(LobbySignInRequest));
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task Bootstrap_ConnectionEndingAfterOpcode14_AbortsWithoutCreatingLogicalConnection()
    {
        var fixture = CreateFixture();
        try
        {
            await fixture.Input.Writer.WriteAsync(new byte[] { 14 });
            await fixture.Input.Writer.CompleteAsync();

            await fixture.Handler.OnConnectedAsync(fixture.Connection);

            Assert.IsFalse(fixture.FactoryCalled);
            Assert.IsTrue(fixture.ConnectionAbortCalled);
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task Bootstrap_InvalidAuthenticationOpcode_AbortsWithoutCreatingLogicalConnection()
    {
        var fixture = CreateFixture();
        try
        {
            await fixture.Input.Writer.WriteAsync(new byte[] { 14, 18 });

            await fixture.Handler.OnConnectedAsync(fixture.Connection);

            Assert.IsFalse(fixture.FactoryCalled);
            Assert.IsTrue(fixture.ConnectionAbortCalled);
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task Bootstrap_CustomHandshakeHandlerRejectsBeforeAuthentication()
    {
        var handshakeHandler = Substitute.For<IClientHandshakeHandler>();
        handshakeHandler.Handle(Arg.Any<ClientHandshakeRequest>())
            .Returns(new ClientHandshakeResponse { ReturnCode = 1 });
        var fixture = CreateFixture(handshakeHandler);
        try
        {
            await fixture.Input.Writer.WriteAsync(new byte[] { 14, 16, 0 });

            await fixture.Handler.OnConnectedAsync(fixture.Connection);

            Assert.IsFalse(fixture.FactoryCalled);
            Assert.IsFalse(fixture.ValidatedReconnectMessage is not null);
            Assert.IsTrue(fixture.ConnectionAbortCalled);
            CollectionAssert.AreEqual(new byte[] { 1 }, await ReadAllAsync(fixture.Output.Reader));
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Bootstrap_ReconnectUsesRawConnectionAndResumesExistingLogicalConnection()
    {
        var initialInput = new Pipe();
        var initialOutput = new Pipe();
        using var initialClosed = new CancellationTokenSource();
        var initial = CreatePhysicalConnection("stable-logical", initialInput, initialOutput.Writer, initialClosed.Token);
        var reconnectInput = new Pipe();
        var reconnectOutput = new Pipe();
        using var reconnectClosed = new CancellationTokenSource();
        var reconnectOutputGate = new StagedPipeWriter(reconnectOutput.Writer);
        var reconnect = CreatePhysicalConnection("physical-reconnect", reconnectInput, reconnectOutputGate, reconnectClosed.Token);
        reconnect.RemoteEndPoint.Returns(new IPEndPoint(IPAddress.Parse("203.0.113.7"), 43500));

        var options = new RaidoConnectionContextOptions
        {
            StatefulReconnectEnabled = true,
            StatefulReconnectTimeout = TimeSpan.FromSeconds(5)
        };
        var tcp = new RaidoTcpConnectionContext(options, NullLoggerFactory.Instance);
        Assert.IsTrue(tcp.TryAttachPhysicalConnection(initial));
        var oldProtocol = new TestClientProtocol(canParseMessages: false);
        var target = new RaidoHubConnectionContext(
            tcp,
            options,
            oldProtocol,
            NullLoggerFactory.Instance,
            TimeProvider.System);
        var session = Substitute.For<IGameWorldSession>();
        session.ConnectionId.Returns("stable-logical");
        session.MasterId.Returns(42u);
        session.SessionClaimId.Returns("claim");
        var gameClient = new GameClient(DisplayMode.ResizedScreen, Language.German, 1024, 768);
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
                Claims = new Dictionary<string, object> { [OpenIddictConstants.Claims.Subject] = "42" }
            }
        });

        initialClosed.Cancel();
        Assert.IsTrue(tcp.Transport.Input.TryRead(out var boundary));
        tcp.Transport.Input.AdvanceTo(boundary.Buffer.End);
        tcp.AcknowledgeInputBoundary();

        var receivedGameMessage = new TaskCompletionSource<RaidoMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var meter = new Meter($"{nameof(ClientConnectionHandlerTests)}-{Guid.NewGuid()}");
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        var lifetimeManager = Substitute.For<IRaidoHubLifetimeManager>();
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        dispatcher.DispatchMessageAsync(Arg.Any<RaidoHubConnectionContext>(), Arg.Any<RaidoMessage>())
            .Returns(callInfo =>
            {
                receivedGameMessage.TrySetResult(callInfo.Arg<RaidoMessage>()!);
                Assert.AreEqual(DisplayMode.FixedScreen, gameClient.DisplayMode);
                Assert.AreEqual(Language.English, gameClient.Language);
                Assert.AreEqual(800, gameClient.ScreenSizeX);
                Assert.AreEqual(600, gameClient.ScreenSizeY);
                callInfo.Arg<RaidoHubConnectionContext>()!.Abort();
                return Task.CompletedTask;
            });
        var connectionHandler = new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            lifetimeManager,
            dispatcher,
            new RaidoMetrics(meterFactory));

        var authentication = Substitute.For<IAuthenticationService>();
        WorldReconnectAuthenticationRequest? authenticationRequest = null;
#pragma warning disable CA2012 // NSubstitute consumes the configured ValueTask exactly once.
        authentication.AuthenticateWorldReconnectAsync(Arg.Any<WorldReconnectAuthenticationRequest>())
            .Returns(callInfo =>
            {
                authenticationRequest = callInfo.Arg<WorldReconnectAuthenticationRequest>();
                return new ValueTask<WorldReconnectAuthenticationResult>(WorldReconnectAuthenticationResult.Success(42));
            });
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
        var freshProtocol = new TestClientProtocol(canParseMessages: true);
        var services = new ServiceCollection()
            .AddScoped<HandshakeProtocol>(_ => CreateProtocol())
            .AddScoped<IClientProtocolResolver>(_ =>
            {
                var resolver = Substitute.For<IClientProtocolResolver>();
                resolver.GetProtocol(742).Returns(freshProtocol);
                return resolver;
            })
            .BuildServiceProvider();
        var connections = new RaidoHubConnectionStore();
        connections.Add(target);
        var reconnectHandler = new WorldReconnectConnectionHandler(
            authentication,
            sessions,
            claims,
            connections,
            connectionHandler,
            services.GetRequiredService<IServiceScopeFactory>(),
            validator,
            NullLogger<WorldReconnectConnectionHandler>.Instance);
        var factory = Substitute.For<IRaidoHubConnectionContextFactory>();
        var factoryCalled = false;
        factory.Create(Arg.Any<ConnectionContext>(), Arg.Any<IRaidoProtocol>(), Arg.Any<bool>())
            .Returns(_ =>
            {
                factoryCalled = true;
                throw new InvalidOperationException("Reconnect must not create a logical candidate.");
            });
        var clientHandler = new ClientConnectionHandler(
            connectionHandler,
            factory,
            services.GetRequiredService<IServiceScopeFactory>(),
            reconnectHandler,
            new ClientHandshakeHandler(),
            Options.Create(new RaidoOptions()),
            NullLogger<ClientConnectionHandler>.Instance);

        try
        {
            await reconnectInput.Writer.WriteAsync(new byte[] { 14, 16, 1 });
            var clientTask = clientHandler.OnConnectedAsync(reconnect);
            await reconnectOutputGate.ResponseFlushStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            await reconnectInput.Writer.WriteAsync(new byte[] { 0x55 });
            reconnectOutputGate.ReleaseResponse();

            var logicalTask = connectionHandler.ConnectAsync(target);
            await clientTask;
            var gameMessage = await receivedGameMessage.Task.WaitAsync(TimeSpan.FromSeconds(1));
            await logicalTask;
            var handshakeBytes = await ReadAllAsync(reconnectOutput.Reader);

            Assert.IsFalse(factoryCalled);
            Assert.IsNotNull(authenticationRequest);
            Assert.AreEqual("203.0.113.7", authenticationRequest!.RemoteAddress!.ToString());
            Assert.AreEqual("physical-reconnect", authenticationRequest.ConnectionId);
            Assert.AreSame(session, target.Features.Get<Hagalaz.Services.GameWorld.Features.ISessionFeature>()!.Session);
            Assert.AreSame(character, target.Features.Get<ICharacterFeature>()!.Character);
            Assert.AreSame(freshProtocol, target.Protocol);
            Assert.IsInstanceOfType(gameMessage, typeof(TestGameMessage));
            CollectionAssert.AreEqual(new uint[] { 1, 2, 3, 4 }, freshProtocol.ReceivedSeed);
            Assert.AreEqual(4612, handshakeBytes.Length);
            Assert.AreEqual(0, handshakeBytes[0]);
            Assert.AreEqual(15, handshakeBytes[1]);
            Assert.AreEqual(0x12, handshakeBytes[2]);
            Assert.AreEqual(0, handshakeBytes[3]);
            await authentication.DidNotReceive().SignInWorldAsync(Arg.Any<SignInRequest>());
            await sessions.DidNotReceive().TryAddWorldSession(Arg.Any<uint>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
        finally
        {
            target.Abort();
            await target.CleanupAsync();
            await services.DisposeAsync();
            await CompleteAsync(initialInput, initialOutput);
            await CompleteAsync(reconnectInput, reconnectOutput);
            meter.Dispose();
        }
    }

    [TestMethod]
    public async Task ReadMessageAsync_ConsumesOpcode14ButCanRetainFreshAuthenticationMessage()
    {
        var (connection, input, output) = CreateConnection();
        try
        {
            await input.Writer.WriteAsync(new byte[] { 14, 16, 0 });
            var protocol = CreateProtocol();

            var handshake = await ClientConnectionHandler.ReadMessageAsync(
                connection, protocol, null, CancellationToken.None, shouldConsume: static _ => true,
                isValidOpcode: static opcode => opcode == 14);
            var authentication = await ClientConnectionHandler.ReadMessageAsync(
                connection, protocol, null, CancellationToken.None, shouldConsume: static _ => false,
                isValidOpcode: static opcode => opcode is 16 or 19);

            Assert.IsInstanceOfType(handshake, typeof(ClientHandshakeRequest));
            Assert.IsInstanceOfType(authentication, typeof(WorldSignInRequest));
            Assert.IsTrue(input.Reader.TryRead(out var remaining));
            CollectionAssert.AreEqual(new byte[] { 16, 0 }, remaining.Buffer.ToArray());
            input.Reader.AdvanceTo(remaining.Buffer.End);
        }
        finally
        {
            await CompleteAsync(input, output);
        }
    }

    [TestMethod]
    public async Task ReadMessageAsync_CanceledWhileWaitingForAuthentication_StopsWithoutCreatingConnection()
    {
        var (connection, input, output) = CreateConnection();
        try
        {
            await input.Writer.WriteAsync(new byte[] { 14 });
            var protocol = CreateProtocol();
            await ClientConnectionHandler.ReadMessageAsync(
                connection, protocol, null, CancellationToken.None, shouldConsume: static _ => true,
                isValidOpcode: static opcode => opcode == 14);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                ClientConnectionHandler.ReadMessageAsync(
                    connection, protocol, null, cancellation.Token, shouldConsume: static _ => false,
                    isValidOpcode: static opcode => opcode is 16 or 19).AsTask());
        }
        finally
        {
            await CompleteAsync(input, output);
        }
    }

    private static TestFixture CreateFixture(IClientHandshakeHandler? handshakeHandler = null)
    {
        var input = new Pipe();
        var output = new Pipe();
        var connection = Substitute.For<ConnectionContext>();
        var transport = Substitute.For<IDuplexPipe>();
        transport.Input.Returns(input.Reader);
        transport.Output.Returns(output.Writer);
        connection.Transport.Returns(transport);
        connection.ConnectionId.Returns("physical");
        connection.ConnectionClosed.Returns(CancellationToken.None);
        var connectionAbortCalled = false;
        connection.When(x => x.Abort()).Do(_ => connectionAbortCalled = true);
        connection.When(x => x.Abort(Arg.Any<ConnectionAbortedException>())).Do(_ => connectionAbortCalled = true);

        var meter = new Meter($"{nameof(ClientConnectionHandlerTests)}-{Guid.NewGuid()}");
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        var lifetimeManager = Substitute.For<IRaidoHubLifetimeManager>();
        var dispatcher = Substitute.For<IRaidoDispatcher>();
        RaidoMessage? dispatchedMessage = null;
        dispatcher.DispatchMessageAsync(Arg.Any<RaidoHubConnectionContext>(), Arg.Any<RaidoMessage>())
            .Returns(callInfo =>
            {
                dispatchedMessage = callInfo.Arg<RaidoMessage>();
                callInfo.Arg<RaidoHubConnectionContext>()!.Abort();
                return Task.CompletedTask;
            });
        var connectionHandler = new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            lifetimeManager,
            dispatcher,
            new RaidoMetrics(meterFactory));
        var factory = Substitute.For<IRaidoHubConnectionContextFactory>();
        var factoryCalled = false;
        var createdStatefulReconnect = false;
        factory.Create(Arg.Any<ConnectionContext>(), Arg.Any<IRaidoProtocol>(), Arg.Any<bool>())
            .Returns(callInfo =>
            {
                factoryCalled = true;
                createdStatefulReconnect = callInfo.ArgAt<bool>(2);
                var options = new RaidoConnectionContextOptions
                {
                    StatefulReconnectEnabled = createdStatefulReconnect,
                    StatefulReconnectTimeout = TimeSpan.FromSeconds(5)
                };
                var tcp = new RaidoTcpConnectionContext(options, NullLoggerFactory.Instance);
                Assert.IsTrue(tcp.TryAttachPhysicalConnection(connection));
                return new RaidoHubConnectionContext(
                    tcp, options, callInfo.Arg<IRaidoProtocol>()!, NullLoggerFactory.Instance, TimeProvider.System);
            });

        var services = new ServiceCollection()
            .AddScoped<HandshakeProtocol>(_ => CreateProtocol())
            .BuildServiceProvider();
        var validator = Substitute.For<IHandshakeValidator<WorldReconnectRequest>>();
        WorldReconnectRequest? validatedReconnectMessage = null;
        validator.Validate(Arg.Any<WorldReconnectRequest>()).Returns(callInfo =>
        {
            validatedReconnectMessage = callInfo.Arg<WorldReconnectRequest>();
            return ClientSignInResponse.Outdated;
        });
        var reconnectHandler = new WorldReconnectConnectionHandler(
            Substitute.For<IAuthenticationService>(),
            Substitute.For<IGameSessionService>(),
            Substitute.For<IGameSessionClaimStore>(),
            new RaidoHubConnectionStore(),
            connectionHandler,
            services.GetRequiredService<IServiceScopeFactory>(),
            validator,
            NullLogger<WorldReconnectConnectionHandler>.Instance);

        var handler = new ClientConnectionHandler(
            connectionHandler,
            factory,
            services.GetRequiredService<IServiceScopeFactory>(),
            reconnectHandler,
            handshakeHandler ?? new ClientHandshakeHandler(),
            Options.Create(new RaidoOptions()),
            NullLogger<ClientConnectionHandler>.Instance);

        return new TestFixture(
            handler,
            connection,
            input,
            output,
            meter,
            () => connectionAbortCalled,
            () => factoryCalled,
            () => createdStatefulReconnect,
            () => validatedReconnectMessage,
            () => dispatchedMessage);
    }

    private static HandshakeProtocol CreateProtocol() =>
        new(new TestHandshakeCodec(), Options.Create(new ServerConfig { ClientRevision = 742 }));

    private static (ConnectionContext Connection, Pipe Input, Pipe Output) CreateConnection()
    {
        var input = new Pipe();
        var output = new Pipe();
        var transport = Substitute.For<IDuplexPipe>();
        transport.Input.Returns(input.Reader);
        transport.Output.Returns(output.Writer);
        var connection = Substitute.For<ConnectionContext>();
        connection.Transport.Returns(transport);
        return (connection, input, output);
    }

    private static ConnectionContext CreatePhysicalConnection(
        string connectionId,
        Pipe input,
        PipeWriter output,
        CancellationToken closed)
    {
        var transport = Substitute.For<IDuplexPipe>();
        transport.Input.Returns(input.Reader);
        transport.Output.Returns(output);
        var connection = Substitute.For<ConnectionContext>();
        connection.ConnectionId.Returns(connectionId);
        connection.Features.Returns(new FeatureCollection());
        connection.Transport.Returns(transport);
        connection.ConnectionClosed.Returns(closed);
        return connection;
    }

    private static async Task<byte[]> ReadAllAsync(PipeReader reader)
    {
        var result = await reader.ReadAsync();
        var bytes = result.Buffer.ToArray();
        reader.AdvanceTo(result.Buffer.End);
        return bytes;
    }

    private static async Task CompleteAsync(Pipe input, Pipe output)
    {
        await input.Reader.CompleteAsync();
        await input.Writer.CompleteAsync();
        await output.Reader.CompleteAsync();
        await output.Writer.CompleteAsync();
    }

    private sealed class TestFixture(
        ClientConnectionHandler handler,
        ConnectionContext connection,
        Pipe input,
        Pipe output,
        Meter meter,
        Func<bool> abortCalled,
        Func<bool> factoryCalled,
        Func<bool> createdStatefulReconnect,
        Func<WorldReconnectRequest?> validatedReconnectMessage,
        Func<RaidoMessage?> dispatchedMessage) : IAsyncDisposable
    {
        public ClientConnectionHandler Handler { get; } = handler;
        public ConnectionContext Connection { get; } = connection;
        public Pipe Input { get; } = input;
        public Pipe Output { get; } = output;
        public bool ConnectionAbortCalled => abortCalled();
        public bool FactoryCalled => factoryCalled();
        public bool CreatedStatefulReconnect => createdStatefulReconnect();
        public WorldReconnectRequest? ValidatedReconnectMessage => validatedReconnectMessage();
        public RaidoMessage? DispatchedMessage => dispatchedMessage();

        public async ValueTask DisposeAsync()
        {
            Connection.Abort();
            await Input.Reader.CompleteAsync();
            await Input.Writer.CompleteAsync();
            await Output.Reader.CompleteAsync();
            await Output.Writer.CompleteAsync();
            meter.Dispose();
        }
    }

    private sealed class TestHandshakeCodec : IRaidoCodec<HandshakeProtocol>
    {
        private readonly WorldReconnectResponseEncoder _reconnectEncoder =
            new(new CharacterLocationService());

        public bool TryDecodeMessage(int opcode, in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            message = opcode switch
            {
                14 => ClientHandshakeRequest.Instance,
                16 when input.FirstSpan.Length > 0 && input.FirstSpan[0] == 1 => new WorldReconnectRequest
                {
                    ClientRevision = 742,
                    Login = "login",
                    Password = "password",
                    IsaacSeed = new uint[] { 1, 2, 3, 4 },
                    DisplayMode = DisplayMode.FixedScreen,
                    Language = Language.English,
                    ClientSizeX = 800,
                    ClientSizeY = 600
                },
                16 => new WorldSignInRequest(),
                19 => new LobbySignInRequest(),
                _ => null
            };
            return message is not null;
        }

        public bool TryEncodeMessage<TMessage>(TMessage message, IRaidoMessageBinaryWriter output)
            where TMessage : RaidoMessage
        {
            if (message is ClientHandshakeResponse handshakeResponse)
            {
                output.SetOpcode(handshakeResponse.ReturnCode);
                return true;
            }

            if (message is ClientSignInResponse signInResponse)
            {
                output.SetOpcode(signInResponse.GetOpcode());
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

    private sealed class TestGameMessage : RaidoMessage
    {
    }

    private sealed class TestClientProtocol(bool canParseMessages) : IClientProtocol
    {
        public string Name => "test-client";
        public int Version => 742;
        public uint[]? ReceivedSeed { get; private set; }

        public bool TryParseMessage(
            in ReadOnlySequence<byte> input,
            ref SequencePosition consumed,
            ref SequencePosition examined,
            out RaidoMessage? message)
        {
            if (!canParseMessages || input.IsEmpty)
            {
                consumed = input.Start;
                examined = input.End;
                message = null;
                return false;
            }

            consumed = input.End;
            examined = input.End;
            message = new TestGameMessage();
            return true;
        }

        public void WriteMessage(RaidoMessage message, IBufferWriter<byte> output)
        {
        }

        public ReadOnlyMemory<byte> GetMessageBytes(RaidoMessage message) => ReadOnlyMemory<byte>.Empty;

        public bool IsVersionSupported(int version) => version == Version;

        public void SetEncryptionSeed(uint[] seed) => ReceivedSeed = seed.ToArray();
    }

    private sealed class StagedPipeWriter(PipeWriter output) : PipeWriter
    {
        private readonly ArrayBufferWriter<byte> _buffer = new();
        private readonly TaskCompletionSource<FlushResult> _responseRelease =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _flushCount;

        public TaskCompletionSource ResponseFlushStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void ReleaseResponse() =>
            _responseRelease.TrySetResult(new FlushResult(isCanceled: false, isCompleted: false));

        public override void Advance(int bytes) => _buffer.Advance(bytes);

        public override Memory<byte> GetMemory(int sizeHint = 0) => _buffer.GetMemory(sizeHint);

        public override Span<byte> GetSpan(int sizeHint = 0) => _buffer.GetSpan(sizeHint);

        public override void CancelPendingFlush() => ReleaseResponse();

        public override async ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
        {
            var bytes = _buffer.WrittenMemory.ToArray();
            _buffer.Clear();
            if (Interlocked.Increment(ref _flushCount) == 2)
            {
                ResponseFlushStarted.TrySetResult();
                await _responseRelease.Task.WaitAsync(cancellationToken);
            }

            output.Write(bytes);
            return await output.FlushAsync(cancellationToken);
        }

        public override void Complete(Exception? exception = null) => output.Complete(exception);
    }
}
