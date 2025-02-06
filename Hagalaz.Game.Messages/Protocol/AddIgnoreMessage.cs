using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class AddIgnoreMessage : RaidoMessage
    {
        public required string DisplayName { get; init; }
    }
}
