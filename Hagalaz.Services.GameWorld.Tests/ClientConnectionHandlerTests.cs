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
using Microsoft.Extensions.Logging;
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
    public async Task RawClassificationConsumesReconnectHandshake()
    {
        var (connection, input, output) = CreateConnection();
        try
        {
            await input.Writer.WriteAsync(new byte[] { 16, 1 });
            var protocol = CreateProtocol();

            var message = await ClientConnectionHandler.ReadInitialMessageAsync(
                connection,
                protocol,
                maximumMessageSize: null,
                CancellationToken.None);

            Assert.IsInstanceOfType<WorldReconnectRequest>(message);
            if (input.Reader.TryRead(out var remaining))
            {
                Assert.IsTrue(remaining.Buffer.IsEmpty);
                input.Reader.AdvanceTo(remaining.Buffer.End);
            }
        }
        finally
        {
            await input.Reader.CompleteAsync();
            await input.Writer.CompleteAsync();
            await output.Reader.CompleteAsync();
            await output.Writer.CompleteAsync();
        }
    }

    [TestMethod]
    public async Task RawClassificationRetainsFreshHandshakeForLogicalHandler()
    {
        var (connection, input, output) = CreateConnection();
        try
        {
            await input.Writer.WriteAsync(new byte[] { 16, 0 });
            var protocol = CreateProtocol();

            var message = await ClientConnectionHandler.ReadInitialMessageAsync(
                connection,
                protocol,
                maximumMessageSize: null,
                CancellationToken.None);

            Assert.IsInstanceOfType<WorldSignInRequest>(message);
            Assert.IsTrue(input.Reader.TryRead(out var remaining));
            CollectionAssert.AreEqual(new byte[] { 16, 0 }, remaining.Buffer.ToArray());
            input.Reader.AdvanceTo(remaining.Buffer.End);
        }
        finally
        {
            await input.Reader.CompleteAsync();
            await input.Writer.CompleteAsync();
            await output.Reader.CompleteAsync();
            await output.Writer.CompleteAsync();
        }
    }

    [TestMethod]
    public async Task OnConnectedAsync_ReconnectDoesNotCreateLogicalCandidate()
    {
        var (connection, input, output) = CreateConnection();
        using var meter = new Meter($"{nameof(ClientConnectionHandlerTests)}-{Guid.NewGuid()}");
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        var raidoHandler = new RaidoHubConnectionHandler(
            NullLoggerFactory.Instance,
            Options.Create(new RaidoOptions()),
            Substitute.For<IRaidoHubLifetimeManager>(),
            Substitute.For<IRaidoDispatcher>(),
            new RaidoMetrics(meterFactory));
        await using var provider = new ServiceCollection()
            .AddScoped<HandshakeProtocol>(_ => CreateProtocol())
            .BuildServiceProvider();
        var validator = Substitute.For<IHandshakeValidator<WorldReconnectRequest>>();
        validator.Validate(Arg.Any<WorldReconnectRequest>()).Returns(ClientSignInResponse.Outdated);
        var factory = Substitute.For<IRaidoHubConnectionContextFactory>();
        var reconnectHandler = new WorldReconnectConnectionHandler(
            Substitute.For<IAuthenticationService>(),
            Substitute.For<IGameSessionService>(),
            Substitute.For<IGameSessionClaimStore>(),
            new RaidoHubConnectionStore(),
            raidoHandler,
            provider.GetRequiredService<IServiceScopeFactory>(),
            validator,
            NullLogger<WorldReconnectConnectionHandler>.Instance);
        var handler = new ClientConnectionHandler(
            raidoHandler,
            factory,
            provider.GetRequiredService<IServiceScopeFactory>(),
            reconnectHandler,
            Options.Create(new RaidoOptions()),
            NullLogger<ClientConnectionHandler>.Instance);

        try
        {
            await input.Writer.WriteAsync(new byte[] { 16, 1 });
            await handler.OnConnectedAsync(connection);

            factory.DidNotReceiveWithAnyArgs().Create(null!, null!, false);
        }
        finally
        {
            connection.Abort();
            await input.Reader.CompleteAsync();
            await input.Writer.CompleteAsync();
            await output.Reader.CompleteAsync();
            await output.Writer.CompleteAsync();
        }
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

    private sealed class TestHandshakeCodec : IRaidoCodec<HandshakeProtocol>
    {
        public bool TryDecodeMessage(int opcode, in ReadOnlySequence<byte> input, out RaidoMessage? message)
        {
            message = opcode == 16 && input.FirstSpan[0] == 1
                ? new WorldReconnectRequest()
                : opcode == 16
                    ? new WorldSignInRequest()
                    : null;
            return message is not null;
        }

        public bool TryEncodeMessage<TMessage>(TMessage message, IRaidoMessageBinaryWriter output)
            where TMessage : RaidoMessage
        {
            if (message is ClientSignInResponse response)
            {
                output.SetOpcode(response.GetOpcode());
                return true;
            }

            return false;
        }
    }
}
