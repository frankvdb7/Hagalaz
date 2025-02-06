using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class DrawSpriteComponentMessageEncoder : IRaidoMessageEncoder<DrawSpriteComponentMessage>
    {
        public void EncodeMessage(DrawSpriteComponentMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(97)
            .SetSize(RaidoMessageSize.Fixed)
            .WriteInt32LittleEndian(message.SpriteId)
            .WriteInt32MiddleEndian(message.ComponentId << 16 | message.ChildId);
    }
}
