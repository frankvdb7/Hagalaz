using Hagalaz.Game.Messages.Protocol.Map;
using Raido.Common.Protocol;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class SetGroundItemCountMessageEncoder : IRaidoMessageEncoder<SetGroundItemCountMessage>
    {
        public void EncodeMessage(SetGroundItemCountMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(41)
            .SetSize(RaidoMessageSize.Fixed)
            .WriteByte((byte)((message.PartLocalX & 0x7) << 4 | message.PartLocalY & 0x7))
            .WriteInt16BigEndian((short)message.ItemId)
            .WriteInt16BigEndian((short)message.OldCount)
            .WriteInt16BigEndian((short)message.NewCount);
    }
}
