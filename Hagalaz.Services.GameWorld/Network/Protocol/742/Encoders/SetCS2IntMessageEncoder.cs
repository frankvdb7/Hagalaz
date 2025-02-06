using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class SetCS2IntMessageEncoder : IRaidoMessageEncoder<SetCS2IntMessage>
    {
        public void EncodeMessage(SetCS2IntMessage message, IRaidoMessageBinaryWriter output)
        {
            if (message.Value <= sbyte.MaxValue && message.Value >= sbyte.MinValue)
            {
                output
                    .SetOpcode(22)
                    .SetSize(RaidoMessageSize.Fixed)
                    .WriteInt16BigEndianA((short)message.Id)
                    .WriteByteS((byte)message.Value);
            }
            else
            {
                output
                    .SetOpcode(152)
                    .SetSize(RaidoMessageSize.Fixed)
                    .WriteInt16LittleEndianA((short)message.Id)
                    .WriteInt32LittleEndian(message.Value);
            }
        }
    }
}
