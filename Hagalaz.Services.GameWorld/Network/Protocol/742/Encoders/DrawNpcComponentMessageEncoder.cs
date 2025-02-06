using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class DrawNpcComponentMessageEncoder : IRaidoMessageEncoder<DrawNpcComponentMessage>
    {
        public void EncodeMessage(DrawNpcComponentMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(134)
            .SetSize(RaidoMessageSize.Fixed)
            .WriteInt32LittleEndian(message.NpcId)
            .WriteInt32BigEndian(message.ComponentId << 16 | message.ChildId);
    }
}
