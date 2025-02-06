using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class DrawItemContainerMessageEncoder : IRaidoMessageEncoder<DrawItemContainerMessage>
    {
        public void EncodeMessage(DrawItemContainerMessage message, IRaidoMessageBinaryWriter output)
        {
            output
                .SetOpcode(67)
                .SetSize(RaidoMessageSize.VariableShort)
                .WriteInt16BigEndian((short)message.Id)
                .WriteByte((byte)(message.Split ? 1 : 0))
                .WriteInt16BigEndian((short)message.Capacity);

            foreach (var item in message.Items)
            {
                var id = item?.Id ?? -1;
                var count = item?.Count ?? 0;
                output.WriteByteA((byte)(count > 254 ? 255 : count));
                if (count > 254)
                {
                    output.WriteInt32MiddleEndian(count);
                }
                output.WriteInt16BigEndianA((short)(id + 1));
            }
        }
    }
}
