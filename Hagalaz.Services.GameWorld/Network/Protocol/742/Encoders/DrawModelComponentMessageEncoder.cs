using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class DrawModelComponentMessageEncoder : IRaidoMessageEncoder<DrawModelComponentMessage>
    {
        public void EncodeMessage(DrawModelComponentMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(78)
            .SetSize(RaidoMessageSize.Fixed)
            .WriteInt32MiddleEndian(message.ModelId)
            .WriteInt32BigEndian(message.ComponentId << 16 | message.ChildId);

    }
}
