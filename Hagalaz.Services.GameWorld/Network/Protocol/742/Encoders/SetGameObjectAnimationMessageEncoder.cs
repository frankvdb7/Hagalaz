using Hagalaz.Game.Messages.Protocol.Map;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class SetGameObjectAnimationMessageEncoder : IRaidoMessageEncoder<SetGameObjectAnimationMessage>
    {
        public void EncodeMessage(SetGameObjectAnimationMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(157)
            .SetSize(RaidoMessageSize.Fixed)
            .WriteInt32MiddleEndian(message.AnimationId)
            .WriteByteS((byte)(message.Rotation & 0x3 | (byte)message.Shape << 2))
            .WriteByte((byte)((message.PartLocalX & 0x7) << 4 | message.PartLocalY & 0x7));
    }
}
