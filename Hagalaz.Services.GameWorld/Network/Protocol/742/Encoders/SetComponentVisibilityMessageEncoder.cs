using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class SetComponentVisibilityMessageEncoder : IRaidoMessageEncoder<SetComponentVisibilityMessage>
    {
        public void EncodeMessage(SetComponentVisibilityMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(113)
            .SetSize(RaidoMessageSize.Fixed)
            .WriteByteS((byte)(message.Visible ? 1 : 0))
            .WriteInt32BigEndian(message.ComponentId << 16 | message.ChildId);
    }
}
