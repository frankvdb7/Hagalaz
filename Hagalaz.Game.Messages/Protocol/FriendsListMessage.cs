using System.Collections.Generic;
using Hagalaz.Game.Messages.Protocol.Model;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class FriendsListMessage : RaidoMessage
    {
        public required IReadOnlyList<ContactDto> Friends { get; init; }
        public bool Notify { get; init; }
    }
}
