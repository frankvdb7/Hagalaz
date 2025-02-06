using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class DrawCreatureHintIconMessageEncoder : IRaidoMessageEncoder<DrawCreatureHintIconMessage>
    {
        public void EncodeMessage(DrawCreatureHintIconMessage message, IRaidoMessageBinaryWriter output)
        {
            var targetType = message.IsCharacter ? 10 : 1;
            output
                .SetOpcode(74)
                .SetSize(RaidoMessageSize.Fixed)
                .WriteByte((byte)((targetType & 0x1f) | (message.IconIndex << 5)))
                .WriteByte((byte)message.ArrowId)
                .WriteInt16BigEndian((short)message.TargetIndex)
                .WriteInt16BigEndian((short)message.FlashSpeed)
                .WriteInt32BigEndian(message.IconModelId);
            }
    }
}
