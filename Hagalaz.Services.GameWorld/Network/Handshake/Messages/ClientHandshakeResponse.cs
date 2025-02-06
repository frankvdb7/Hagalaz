using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Handshake.Messages
{
    public class ClientHandshakeResponse : RaidoMessage
    {
        public byte ReturnCode { get; init; }
    }
}
