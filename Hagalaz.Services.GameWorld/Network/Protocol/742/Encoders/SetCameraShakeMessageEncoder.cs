using Hagalaz.Game.Messages.Protocol;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class SetCameraShakeMessageEncoder : IRaidoMessageEncoder<SetCameraShakeMessage>
    {
        public void EncodeMessage(SetCameraShakeMessage message, IRaidoMessageBinaryWriter output)
        {
            if (message.Reset)
            {
                output.SetOpcode(28).SetSize(RaidoMessageSize.Fixed);
            }
            else
            {
                output
                    .SetOpcode(91)
                    .SetSize(RaidoMessageSize.Fixed)
                    .WriteInt16BigEndianA((short)message.UpDelta)
                    .WriteByte((byte)message.DownDelta)
                    .WriteByteC((byte)message.LeftDelta)
                    .WriteByteC((byte)message.Index)
                    .WriteByteS((byte)message.RightDelta);
            }
        }
    }
}
