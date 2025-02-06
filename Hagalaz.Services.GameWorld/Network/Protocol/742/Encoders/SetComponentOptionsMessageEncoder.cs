using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class SetComponentOptionsMessageEncoder : IRaidoMessageEncoder<SetComponentOptionsMessage>
    {
        public void EncodeMessage(SetComponentOptionsMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(118)
            .SetSize(RaidoMessageSize.Fixed)
            .WriteInt16BigEndian((short)message.MinimumSlot)
            .WriteInt32LittleEndian(message.Value)
            .WriteInt32MixedEndian(message.Id << 16 | message.ChildId)
            .WriteInt16LittleEndian((short)message.MaximumSlot);
    }
}
