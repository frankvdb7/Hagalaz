using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class DrawStringComponentMessageEncoder : IRaidoMessageEncoder<DrawStringComponentMessage>
    {
        public void EncodeMessage(DrawStringComponentMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(107)
            .SetSize(RaidoMessageSize.VariableShort)
            .WriteString(message.Value)
            .WriteInt32BigEndian(message.ComponentId << 16 | message.ChildId);
    }
}
