using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class RemoveHintIconMessageEncoder : IRaidoMessageEncoder<RemoveHintIconMessage>
    {
        public void EncodeMessage(RemoveHintIconMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(74)
            .SetSize(RaidoMessageSize.Fixed)
            .WriteByte((byte)((0 & 0x1f) | (message.IconIndex << 5)));
    }
}
