using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class DrawFrameComponentMessageEncoder : IRaidoMessageEncoder<DrawFrameComponentMessage>
    {
        public void EncodeMessage(DrawFrameComponentMessage message, IRaidoMessageBinaryWriter output) => output
                .SetOpcode(45)
                .SetSize(RaidoMessageSize.Fixed)
                .WriteInt32MiddleEndian(0) // xtea 
                .WriteByteS((byte)(message.ForceRedraw ? 2 : 0))
                .WriteInt32BigEndian(0) // xtea
                .WriteInt32LittleEndian(0) // xtea
                .WriteInt16BigEndianA((short)message.Id)
                .WriteInt32MixedEndian(0); // xtea
    }
}
