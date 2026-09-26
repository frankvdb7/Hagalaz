using System.Buffers;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Services.GameWorld.Network.Handshake.Encoders;
using Hagalaz.Services.GameWorld.Network.Handshake;
using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.Extensions.Options;
using Raido.Common.Buffers;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class WorldReconnectResponseEncoderTests
{
    [TestMethod]
    public void ReconnectResponseWritesOpcode15AndExactWorldEntryPayload()
    {
        var encoder = new WorldReconnectResponseEncoder(new CharacterLocationService());
        using var buffer = MemoryBufferWriter.Get();
        var writer = new RaidoMessageBinaryWriter(buffer);

        encoder.EncodeMessage(new WorldReconnectResponse
        {
            CharacterIndex = 1,
            CharacterLocation = new Location(3200, 3200, 0, 0)
        }, writer);

        Assert.AreEqual(15, writer.Opcode);
        Assert.AreEqual(RaidoMessageSize.VariableShort, writer.Size);
        Assert.AreEqual(4608, buffer.Length);
    }

    [TestMethod]
    public void HandshakeProtocolFramesReconnectResponseWithA16BitPayloadLength()
    {
        var protocol = new HandshakeProtocol(
            new TestHandshakeCodec(),
            Options.Create(new ServerConfig { ClientRevision = 742 }));

        var bytes = protocol.GetMessageBytes(new WorldReconnectResponse
        {
            CharacterIndex = 1,
            CharacterLocation = new Location(3200, 3200, 0, 0)
        });

        Assert.AreEqual(4611, bytes.Length);
        CollectionAssert.AreEqual(new byte[] { 15, 0x12, 0 }, bytes[..3].ToArray());
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
            if (message is WorldReconnectResponse reconnect)
            {
                _reconnectEncoder.EncodeMessage(reconnect, output);
                return true;
            }

            if (message is ClientSignInResponse response)
            {
                output.SetOpcode(response.GetOpcode());
                return true;
            }

            return false;
        }
    }
}
