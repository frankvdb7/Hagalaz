using Hagalaz.Game.Messages.Protocol.Map;
using Raido.Common.Protocol;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class DrawGraphicMessageEncoder : IRaidoMessageEncoder<DrawGraphicMessage>
    {
        public void EncodeMessage(DrawGraphicMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(133)
            .SetSize(RaidoMessageSize.Fixed)
            .WriteByte((byte)((message.PartLocalX & 0x7) << 4 | message.PartLocalY & 0x7))
            .WriteInt16BigEndian((short)message.GraphicId)
            .WriteInt16BigEndian((short)message.Height)
            .WriteInt16BigEndian((short)message.Delay)
            .WriteInt16BigEndian((short)message.TargetIndex); // the character target index, has something to do with calculating the volume level of the graphic sound
    }
}
