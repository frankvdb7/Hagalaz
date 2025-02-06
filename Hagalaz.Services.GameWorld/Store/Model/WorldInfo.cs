namespace Hagalaz.Services.GameWorld.Store.Model
{
    public record WorldInfo
    {
        public record WorldLocationInfo
        {
            public required string Name { get; init; }
            public required int Flag { get; init; }
        }

        public record WorldSettingsInfo
        {
            public required bool IsMembersOnly { get; init; }
            public required bool IsQuickChatEnabled { get; init; }
            public required bool IsPvP { get; init; }
            public required bool IsLootShareEnabled { get; init; }
            public required bool IsHighlighted { get; init; }
        }
        public required int Id { get; init; }
        public required string Name { get; init; }
        public required string IpAddress { get; init; }
        public required WorldLocationInfo Location { get; init; }
        public required WorldSettingsInfo Settings { get; init; }
        public bool Online { get; set; }
        public int CharacterCount { get; set; }
    }
}