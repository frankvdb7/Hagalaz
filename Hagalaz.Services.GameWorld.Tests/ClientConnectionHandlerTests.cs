using System.Buffers;
using System.IO.Pipelines;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Network;
using Hagalaz.Services.GameWorld.Network.Handshake;
using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Options;
using NSubstitute;
using Raido.Common.Messages;
using Raido.Common.Protocol;

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
            where TMessage : RaidoMessage => false;
    }
}
