using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class SetVarpBitMessageEncoder : IRaidoMessageEncoder<SetVarpBitMessage>
    {
        public void EncodeMessage(SetVarpBitMessage message, IRaidoMessageBinaryWriter output)
        {
            if (message.Value <= sbyte.MaxValue && message.Value >= sbyte.MinValue)
            {
                output
                    .SetOpcode(156)
                    .SetSize(RaidoMessageSize.Fixed)
                    .WriteByteA((byte)message.Value)
                    .WriteInt16LittleEndianA((short)message.Id);
            }
            else
            {
                output
                    .SetOpcode(99)
                    .SetSize(RaidoMessageSize.Fixed)
                    .WriteInt16BigEndianA((short)message.Id)
                    .WriteInt32BigEndian(message.Value);
            }
        }
    }
}
