namespace Hagalaz.Services.GameLogon.Services.Model
{
    public record FriendsChatSettingsDto
    {
        public string ChatName { get; init; } = default!;
        public FriendsChatRank RankEnter { get; init; }
        public FriendsChatRank RankKick { get; init; }
        public FriendsChatRank RankTalk { get; init; }
    }
}