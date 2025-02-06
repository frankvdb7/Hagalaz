using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class SetSkillMessageEncoder : IRaidoMessageEncoder<SetSkillMessage>
    {
        public void EncodeMessage(SetSkillMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(4)
            .SetSize(RaidoMessageSize.Fixed)
            .WriteByteS((byte)message.Id)
            .WriteByteA((byte)message.Level)
            .WriteInt32LittleEndian(message.Experience);
    }
}
