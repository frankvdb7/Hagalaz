using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class SetSystemUpdateTickMessageEncoder : IRaidoMessageEncoder<SetSystemUpdateTickMessage>
    {
        public void EncodeMessage(SetSystemUpdateTickMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(147)
            .SetSize(RaidoMessageSize.Fixed)
            .WriteInt16BigEndian((short)message.Tick);
    }
}
