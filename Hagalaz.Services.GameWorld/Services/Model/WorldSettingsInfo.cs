namespace Hagalaz.Services.GameWorld.Services.Model
{
    public record WorldSettingsInfo
    {
        public required bool IsMembersOnly { get; init; }
        public required bool IsQuickChatEnabled { get; init; }
        public required bool IsPvP { get; init; }
        public required bool IsLootShareEnabled { get; init; }
        public required bool IsHighLighted { get; init; }
    }
}