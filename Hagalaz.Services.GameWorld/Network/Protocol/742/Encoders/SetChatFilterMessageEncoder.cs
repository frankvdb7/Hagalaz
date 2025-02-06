using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class SetChatFilterMessageEncoder : IRaidoMessageEncoder<SetChatFilterMessage>
    {
        public void EncodeMessage(SetChatFilterMessage message, IRaidoMessageBinaryWriter output)
        {
            if (message.HasPrivateFilter)
            {
                output
                    .SetOpcode(76)
                    .SetSize(RaidoMessageSize.Fixed)
                    .WriteByte((byte)message.PrivateFilter);
            } 
            else
            {
                output
                    .SetOpcode(75)
                    .SetSize(RaidoMessageSize.Fixed)
                    .WriteByteC((byte)message.PublicFilter)
                    .WriteByteS((byte)message.TradeFilter);
            }
        }
    }
}
