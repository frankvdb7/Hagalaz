using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class DrawInterfaceComponentMessageEncoder : IRaidoMessageEncoder<DrawInterfaceComponentMessage>
    {
        public void EncodeMessage(DrawInterfaceComponentMessage message, IRaidoMessageBinaryWriter output) => output
                .SetOpcode(47)
                .SetSize(RaidoMessageSize.Fixed)
                .WriteByteS((byte)message.Transparency)
                .WriteInt16LittleEndianA((short)message.Id)
                .WriteInt32MixedEndian(message.ParentId << 16 | message.ParentSlot)
                .WriteInt32MixedEndian(0) // xtea 1
                .WriteInt32MiddleEndian(0) // xtea 2
                .WriteInt32MixedEndian(0) // xtea 4
                .WriteInt32BigEndian(0);
    }
}
