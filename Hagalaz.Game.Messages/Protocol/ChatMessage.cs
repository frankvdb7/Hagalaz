using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class ChatMessage : RaidoMessage
    {
        public required ChatMessageType Type { get; init; }
        public string? DisplayName { get; init; }
        public string? PreviousDisplayName { get; init; }
        public required string Text { get; init; } = default!;
    }
}
