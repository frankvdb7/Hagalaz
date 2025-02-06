using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class SetWeightMessageEncoder : IRaidoMessageEncoder<SetWeightMessage>
    {
        public void EncodeMessage(SetWeightMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(54)
            .SetSize(RaidoMessageSize.Fixed)
            .WriteInt16BigEndian((short)message.Value);
    }
}
