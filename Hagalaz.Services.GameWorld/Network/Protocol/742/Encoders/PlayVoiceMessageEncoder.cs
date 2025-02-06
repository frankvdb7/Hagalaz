using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class PlayVoiceMessageEncoder : IRaidoMessageEncoder<PlayVoiceMessage>
    {
        public void EncodeMessage(PlayVoiceMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(26)
            .SetSize(RaidoMessageSize.Fixed)
            .WriteInt16BigEndian((short)message.Id)
            .WriteByte((byte)message.RepeatCount)
            .WriteInt16BigEndian((short)message.Delay)
            .WriteByte((byte)message.Volume);
    }
}
