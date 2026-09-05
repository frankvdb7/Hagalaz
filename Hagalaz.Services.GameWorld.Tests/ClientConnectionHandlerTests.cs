using System.Buffers;
using System.Diagnostics.Metrics;
using System.IO.Pipelines;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Network;
using Hagalaz.Services.GameWorld.Network.Handshake;
using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
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

    private static TestFixture CreateFixture()
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
        public bool TryDecodeMessage(int opcode, in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            message = opcode switch
            {
                14 => ClientHandshakeRequest.Instance,
                16 when input.FirstSpan.Length > 0 && input.FirstSpan[0] == 1 => new WorldReconnectRequest(),
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

            return false;
        }
    }
}
