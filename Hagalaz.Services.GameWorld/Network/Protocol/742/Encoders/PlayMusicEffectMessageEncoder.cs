using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class PlayMusicEffectMessageEncoder : IRaidoMessageEncoder<PlayMusicEffectMessage>
    {
        public void EncodeMessage(PlayMusicEffectMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(119)
            .SetSize(RaidoMessageSize.Fixed)
            .WriteInt24BigEndian(0) // unused
            .WriteInt16BigEndian((short)message.Id)
            .WriteByteS((byte)message.Volume);
    }
}
