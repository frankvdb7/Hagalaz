using Hagalaz.Game.Messages.Protocol.Map;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class RemoveGameObjectMessageEncoder : IRaidoMessageEncoder<RemoveGameObjectMessage>
    {
        public void EncodeMessage(RemoveGameObjectMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(37)
            .SetSize(RaidoMessageSize.Fixed)
            .WriteByteC((byte)((message.PartLocalX & 0x7) << 4 | message.PartLocalY & 0x7))
            .WriteByteA((byte)(message.Rotation & 0x3 | (byte)message.Shape << 2));

    }
}
