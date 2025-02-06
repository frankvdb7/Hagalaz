using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class SetComponentAnimationMessageEncoder : IRaidoMessageEncoder<SetComponentAnimationMessage>
    {
        public void EncodeMessage(SetComponentAnimationMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(17)
            .SetSize(RaidoMessageSize.Fixed)
            .WriteInt32BigEndian(message.ComponentId << 16 | message.ChildId)
            .WriteInt32MixedEndian(message.AnimationId);
    }
}
