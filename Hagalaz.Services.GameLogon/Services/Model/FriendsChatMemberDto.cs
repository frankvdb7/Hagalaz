namespace Hagalaz.Services.GameLogon.Services.Model
{
    public record FriendsChatMemberDto
    {
        public string DisplayName { get; set; } = null!;
        public string? PreviousDisplayName { get; set; } = null!;
        public int WorldId { get; set; }
        public string ChatName { get; set; } = null!;
        public long SessionId { get; set; }
    }
}