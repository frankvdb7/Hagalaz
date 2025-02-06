using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class DrawItemComponentMessageEncoder : IRaidoMessageEncoder<DrawItemComponentMessage>
    {
        public void EncodeMessage(DrawItemComponentMessage message, IRaidoMessageBinaryWriter output) => output
            .SetOpcode(42)
            .SetSize(RaidoMessageSize.Fixed)
            .WriteInt32LittleEndian(message.ComponentId << 16 | message.ChildId)
            .WriteInt16LittleEndian((short)message.ItemId)
            .WriteInt32BigEndian(message.ItemCount);
    }
}
