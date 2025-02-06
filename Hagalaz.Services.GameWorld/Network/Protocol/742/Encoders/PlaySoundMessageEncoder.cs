using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class PlaySoundMessageEncoder : IRaidoMessageEncoder<PlaySoundMessage>
    {
        public void EncodeMessage(PlaySoundMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(15)
            .SetSize(RaidoMessageSize.Fixed)
            .WriteInt16BigEndian((short)message.Id)
            .WriteByte((byte)message.RepeatCount)
            .WriteInt16BigEndian((short)message.Delay)
            .WriteByte((byte)message.Volume)
            .WriteInt16BigEndian((short)message.PlaybackSpeed);
    }
}
