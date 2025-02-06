using Raido.Common.Protocol;

namespace Hagalaz.Services.GameUpdate.Network.Messages
{
    public class AuthStatusChangedMessage : RaidoMessage
    {
        public bool Authenticated { get; init; }
    }
}