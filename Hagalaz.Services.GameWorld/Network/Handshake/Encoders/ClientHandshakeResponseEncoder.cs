using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Handshake.Encoders
{
    public class ClientHandshakeResponseEncoder : IRaidoMessageEncoder<ClientHandshakeResponse>
    {
        public void EncodeMessage(ClientHandshakeResponse message, IRaidoMessageBinaryWriter output) => output.SetOpcode(message.ReturnCode);
    }
}
