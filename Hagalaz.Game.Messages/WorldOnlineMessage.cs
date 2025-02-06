namespace Hagalaz.Game.Messages
{
    public record WorldOnlineMessage
    {
        public record WorldSettings
        {
            public required bool IsMembersOnly { get; init; }
            public required bool IsQuickChatEnabled { get; init; }
            public required bool IsPvP { get; init; }
            public required bool IsLootShareEnabled { get; init; }
            public required bool IsHighLighted { get; init; }
        }

        public record WorldLocation
        {
            public required string Name { get; init; }
            public required int Flag { get; init; }
        }
        
        public required int Id { get; init; }
        public required string Name { get; init; }
        public required string IpAddress { get; init; }
        public required int CharacterCount { get; init; }
        public required WorldSettings Settings { get; init; }
        public required WorldLocation Location { get; init; }
    }
}
