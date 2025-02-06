using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Handshake.Messages
{
    public class ClientHandshakeRequest : RaidoMessage
    {
        public static ClientHandshakeRequest Instance { get; } = new ClientHandshakeRequest();
    }
}
