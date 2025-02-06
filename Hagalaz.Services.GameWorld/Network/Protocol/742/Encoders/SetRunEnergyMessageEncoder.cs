using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class SetRunEnergyMessageEncoder : IRaidoMessageEncoder<SetRunEnergyMessage>
    {
        public void EncodeMessage(SetRunEnergyMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(69)
            .SetSize(RaidoMessageSize.Fixed)
            .WriteByte((byte)message.Energy);
    }
}
