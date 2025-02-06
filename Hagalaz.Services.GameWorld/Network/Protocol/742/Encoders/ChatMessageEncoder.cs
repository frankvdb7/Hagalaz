using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class ChatMessageEncoder : IRaidoMessageEncoder<ChatMessage>
    {
        public void EncodeMessage(ChatMessage message, IRaidoMessageBinaryWriter output) 
        {
            output
                .SetOpcode(2)
                .SetSize(RaidoMessageSize.VariableByte)
                .WriteInt16BigEndianSmart((short)message.Type)
                .WriteInt32BigEndian(0); // cs2 int

            var hasDisplayName = !string.IsNullOrEmpty(message.DisplayName);
            var hasPreviousDisplayName = !string.IsNullOrEmpty(message.PreviousDisplayName);
            var mask = 0;
            if (hasDisplayName)
            {
                mask |= 0x1;
            }
            if (hasPreviousDisplayName)
            {
                mask |= 0x2;
            }
            output.WriteByte((byte)mask);

            if (hasDisplayName)
            {
                output.WriteString(message.DisplayName!);
            }
            if (hasPreviousDisplayName)
            {
                output.WriteString(message.PreviousDisplayName!);
            }

            output.WriteString(message.Text);
        }
    }
}
