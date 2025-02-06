using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class SetCameraLocationMessageEncoder : IRaidoMessageEncoder<SetCameraLocationMessage>
    {
        public void EncodeMessage(SetCameraLocationMessage message, IRaidoMessageBinaryWriter output)
        {
            if (message.Reset)
            {
                output
                    .SetOpcode(25)
                    .SetSize(RaidoMessageSize.Fixed)
                    .WriteByte(0)
                    .WriteByteS(0)
                    .WriteByteA(0)
                    .WriteInt16LittleEndianA(0)
                    .WriteByteA(0);
            }
            else
            {
                output
                    .SetOpcode(25)
                    .SetSize(RaidoMessageSize.Fixed)
                    .WriteByte((byte)message.SpeedY)
                    .WriteByteS((byte)message.LocalX)
                    .WriteByteA((byte)message.SpeedX)
                    .WriteInt16LittleEndianA((short)(message.Z << 2))
                    .WriteByteA((byte)message.LocalY);
            }
        }
    }
}
