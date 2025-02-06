namespace Hagalaz.Services.GameLogon.Services.Model
{
    public record ContactDto
    {
        public uint MasterId { get; init; }
        public string DisplayName { get; init; } = default!;
        public string? PreviousDisplayName { get; init; }
        public FriendsChatRank? Rank { get; init; }
        public int? WorldId { get; init; }
        public bool? IsFriend { get; init; }
        public ContactSettingsDto? Settings { get; init; }
    }
}