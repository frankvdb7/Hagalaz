using Hagalaz.Game.Messages.Protocol.Map;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class AddGroundItemMessageEncoder : IRaidoMessageEncoder<AddGroundItemMessage>
    {
        public void EncodeMessage(AddGroundItemMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(30)
            .SetSize(RaidoMessageSize.Fixed)
            .WriteInt16LittleEndian((short)message.Count)
            .WriteInt16LittleEndian((short)message.ItemId)
            .WriteByteC((byte)((message.PartLocalX & 0x7) << 4 | message.PartLocalY & 0x7));
    }
}
