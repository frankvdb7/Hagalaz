using Raido.Common.Protocol;

namespace Hagalaz.Services.GameUpdate.Network.Messages
{
    public class HandshakeRequest : RaidoMessage
    {
        public const byte Opcode = 15;

        public int ClientRevision { get; }
        public int ClientRevisionPatch { get; }
        public string ServerToken { get; }

        public HandshakeRequest(int clientRevision, int clientRevisionPatch, string serverToken)
        {
            ClientRevision = clientRevision;
            ClientRevisionPatch = clientRevisionPatch;
            ServerToken = serverToken;
        }
    }
}