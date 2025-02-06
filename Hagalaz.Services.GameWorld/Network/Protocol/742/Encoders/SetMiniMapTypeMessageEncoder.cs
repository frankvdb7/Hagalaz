using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class SetMiniMapTypeMessageEncoder : IRaidoMessageEncoder<SetMiniMapTypeMessage>
    {
        public void EncodeMessage(SetMiniMapTypeMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(159)
            .SetSize(RaidoMessageSize.Fixed)
            .WriteByte((byte)message.MinimapType);
    }
}
