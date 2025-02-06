using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class InterfaceComponentTextInputMessage : RaidoMessage
    {
        public required string Text { get; init; } = default!;
    }
}
