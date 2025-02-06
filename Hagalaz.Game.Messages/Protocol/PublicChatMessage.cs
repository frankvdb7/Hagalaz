using Hagalaz.Game.Abstractions.Authorization;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class PublicChatMessage : RaidoMessage
    {
        public required int TextAnimation { get; init; }
        public required int TextColor { get; init; }
        public required string Text { get; init; } = string.Empty;
        public int? CharacterIndex { get; init; }
        public Permission? Permissions { get; init; }
    }
}