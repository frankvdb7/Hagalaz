using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class RemoveFriendMessage : RaidoMessage
    {
        public required string DisplayName { get; init; }
    }
}
