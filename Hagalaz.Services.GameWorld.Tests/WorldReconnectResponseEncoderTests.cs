using Hagalaz.Services.GameWorld.Network.Handshake.Encoders;
using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Raido.Common.Buffers;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class WorldReconnectResponseEncoderTests
{
    [TestMethod]
    public void EncodeMessage_UsesResponse15AndPreservesCharacterizedPayload()
    {
        var payload = new byte[WorldReconnectResponse.EnterWorldPayloadLength];
        payload[0] = 1;
        payload[^1] = 4;
        var buffer = MemoryBufferWriter.Get();
        try
        {
            var writer = new RaidoMessageBinaryWriter(buffer);
            new WorldReconnectResponseEncoder().EncodeMessage(
                new WorldReconnectResponse { EnterWorldPayload = payload },
                writer);

            Assert.AreEqual(15, writer.Opcode);
            Assert.AreEqual(RaidoMessageSize.VariableShort, writer.Size);
            CollectionAssert.AreEqual(payload, buffer.ToArray());
        }
        finally
        {
            MemoryBufferWriter.Return(buffer);
        }
    }
}
