using Hagalaz.Game.Messages.Protocol.Map;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class RemoveGroundItemMessageEncoder : IRaidoMessageEncoder<RemoveGroundItemMessage>
    {
        public void EncodeMessage(RemoveGroundItemMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(71)
            .SetSize(RaidoMessageSize.Fixed)
            .WriteInt16LittleEndian((short)message.ItemId)
            .WriteByteS((byte)((message.PartLocalX & 0x7) << 4 | message.PartLocalY & 0x7));
    }
}
