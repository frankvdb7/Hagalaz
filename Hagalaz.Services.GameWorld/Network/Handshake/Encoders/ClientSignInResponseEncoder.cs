using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Handshake.Encoders
{
    public class ClientSignInResponseEncoder : IRaidoMessageEncoder<ClientSignInResponse>
    {
        public void EncodeMessage(ClientSignInResponse message, IRaidoMessageBinaryWriter output) => output.SetOpcode(message.GetOpcode());
    }
}
