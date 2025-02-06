using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class SetConfigMessageEncoder : IRaidoMessageEncoder<SetConfigMessage>
    {
        public void EncodeMessage(SetConfigMessage message, IRaidoMessageBinaryWriter output)
        {
            if (message.Value >= sbyte.MinValue && message.Value <= sbyte.MaxValue)
            {
                output
                    .SetOpcode(10)
                    .SetSize(RaidoMessageSize.Fixed)
                    .WriteInt16LittleEndian((short)message.Id)
                    .WriteByte((byte)message.Value);
            } 
            else 
            {
                output
                    .SetOpcode(82)
                    .SetSize(RaidoMessageSize.Fixed)
                    .WriteInt32MixedEndian(message.Value)
                    .WriteInt16BigEndianA((short)message.Id);
            }
        }
    }
}
