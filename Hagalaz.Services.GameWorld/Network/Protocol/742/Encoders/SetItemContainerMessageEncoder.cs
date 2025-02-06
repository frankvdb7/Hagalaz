using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class SetItemContainerMessageEncoder : IRaidoMessageEncoder<SetItemContainerMessage>
    {
        public void EncodeMessage(SetItemContainerMessage message, IRaidoMessageBinaryWriter output)
        {
            output
                .SetOpcode(94)
                .SetSize(RaidoMessageSize.VariableShort)
                .WriteInt16BigEndian((short)message.Id)
                .WriteByte((byte)(message.Split ? 1 : 0));

            foreach (var (slot, item) in message.Items)
            {
                output.WriteInt16BigEndianSmart((short)slot);
                if (item != null)
                {
                    output
                        .WriteInt16BigEndian((short)(item.Id + 1))
                        .WriteByte((byte)(item.Count > 254 ? 255 : item.Count));
                    if (item.Count > 254)
                    {
                        output.WriteInt32BigEndian(item.Count);
                    }
                } 
                else
                {
                    output.WriteInt16BigEndian(0);
                }
            }
        }
    }
}
