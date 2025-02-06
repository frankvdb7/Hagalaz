using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class ConsoleCommandMessage : RaidoMessage
    {
        public required string Command { get; init; } = default!;
    }
}
