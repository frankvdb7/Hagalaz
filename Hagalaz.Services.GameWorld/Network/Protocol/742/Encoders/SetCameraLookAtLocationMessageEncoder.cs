using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class SetCameraLookAtLocationMessageEncoder : IRaidoMessageEncoder<SetCameraLookAtLocationMessage>
    {
        public void EncodeMessage(SetCameraLookAtLocationMessage message, IRaidoMessageBinaryWriter output)
        {
            if (message.Reset)
            {
                output
                    .SetOpcode(109)
                    .SetSize(RaidoMessageSize.Fixed)
                    .WriteInt16LittleEndianA(0)
                    .WriteByte(0)
                    .WriteByteS(0)
                    .WriteByteC(0)
                    .WriteByteS(0);
            }
            else
            {
                output
                    .SetOpcode(109)
                    .SetSize(RaidoMessageSize.Fixed)
                    .WriteInt16LittleEndianA((short)(message.Z << 2))
                    .WriteByte((byte)message.LocalY)
                    .WriteByteS((byte)message.SpeedX)
                    .WriteByteC((byte)message.SpeedY)
                    .WriteByteS((byte)message.LocalX);
            }
        }
    }
}
