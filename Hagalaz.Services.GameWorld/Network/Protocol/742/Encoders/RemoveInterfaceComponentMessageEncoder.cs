using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class RemoveInterfaceComponentMessageEncoder : IRaidoMessageEncoder<RemoveInterfaceComponentMessage>
    {
        public void EncodeMessage(RemoveInterfaceComponentMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(148)
            .SetSize(RaidoMessageSize.Fixed)
            .WriteInt32MixedEndian(message.ParentId << 16 | message.ParentSlot);
    }
}
