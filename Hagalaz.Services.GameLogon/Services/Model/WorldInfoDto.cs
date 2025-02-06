namespace Hagalaz.Services.GameLogon.Services.Model
{
    public record WorldInfoDto
    {
        public int Id { get; init; }
        public string Name { get; init; } = null!;
        public string? IpAddress { get; init; }
        public string CountryName { get; init; } = null!;
        public bool IsMembersOnly { get; init; }
        public bool IsQuickChatEnabled { get; init; }
        public bool IsPvP { get; init; }
        public bool IsLootShareEnabled { get; init; }
        public bool ShouldHighlight { get; init; }
        public int CharacterCount { get; init; }
    }
}