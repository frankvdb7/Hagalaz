using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Network.Handshake.Encoders;
using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Raido.Common.Buffers;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class WorldReconnectResponseEncoderTests
{
    [TestMethod]
    public void EncodeMessage_UsesResponse15AndTheRevision742PlayerEntryPayload()
    {
        var locations = Substitute.For<ICharacterLocationService>();
        locations.FindLocationByIndex(Arg.Any<int>()).Returns(new Location(3200, 3200, 0, 0));
        var buffer = MemoryBufferWriter.Get();
        try
        {
            var writer = new RaidoMessageBinaryWriter(buffer);
            new WorldReconnectResponseEncoder(locations).EncodeMessage(
                new WorldReconnectResponse
                {
                    CharacterIndex = 1,
                    CharacterLocation = new Location(3200, 3200, 0, 0)
                },
                writer);

            Assert.AreEqual(15, writer.Opcode);
            Assert.AreEqual(RaidoMessageSize.VariableShort, writer.Size);
            Assert.AreEqual(4608, buffer.Length);
        }
        finally
        {
            MemoryBufferWriter.Return(buffer);
        }
    }

}
