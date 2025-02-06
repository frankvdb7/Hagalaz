using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class AddFriendMessage : RaidoMessage
    {
        public required string DisplayName { get; init; }
    }
}
