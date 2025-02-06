using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class PlayMusicMessageEncoder : IRaidoMessageEncoder<PlayMusicMessage>
    {
        public void EncodeMessage(PlayMusicMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(39)
            .SetSize(RaidoMessageSize.Fixed)
            .WriteByteS((byte)message.Delay)
            .WriteByteC((byte)message.Volume)
            .WriteInt16BigEndian((short)message.Id);
    }
}
