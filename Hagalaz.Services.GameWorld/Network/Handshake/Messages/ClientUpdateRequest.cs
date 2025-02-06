using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Handshake.Messages
{
    public class ClientUpdateRequest : RaidoMessage
    {
        public static ClientUpdateRequest Instance { get; } = new ClientUpdateRequest();
    }
}
