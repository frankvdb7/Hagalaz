using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class RemoveIgnoreMessage : RaidoMessage
    {
        public required string DisplayName { get; init; }
    }
}
