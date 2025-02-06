using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class DrawLocationHintIconMessageEncoder : IRaidoMessageEncoder<DrawLocationHintIconMessage>
    {
        public void EncodeMessage(DrawLocationHintIconMessage message, IRaidoMessageBinaryWriter output)
        {
            var targetType = message.Direction switch
            {
                HintIconDirection.West => 3,
                HintIconDirection.East => 4,
                HintIconDirection.South => 5,
                HintIconDirection.North => 6,
                _ => 2
            };
            output
                .SetOpcode(74)
                .SetSize(RaidoMessageSize.Fixed)
                .WriteByte((byte)((targetType & 0x1f) | (message.IconIndex << 5)))
                .WriteByte((byte)message.ArrowId)
                .WriteByte((byte)message.Z)
                .WriteInt16BigEndian((short)message.X)
                .WriteInt16BigEndian((short)message.Y)
                .WriteByte((byte)((message.Height * 4) >> 2))
                .WriteInt16BigEndian((short)message.ViewDistance)
                .WriteInt32BigEndian(message.IconModelId);
            }
    }
}
