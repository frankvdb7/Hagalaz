using Raido.Common.Messages;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders
{
    public class PingMessageEncoder : IRaidoMessageEncoder<PingMessage>
    {
        public void EncodeMessage(PingMessage message, IRaidoMessageBinaryWriter output) => output.SetOpcode(12).SetSize(RaidoMessageSize.Fixed);
    }
}
