using Hagalaz.Game.Abstractions.Features.FriendsChat;

namespace Hagalaz.Game.Network.Model
{
    public record FriendsChatSettingsDto
    {
        public string? ChatName { get; init; } 
        public FriendsChatRank? RankEnter { get; init; }
        public FriendsChatRank? RankKick { get; init; }
        public FriendsChatRank? RankTalk { get; init; }
        public FriendsChatRank? RankLoot { get; init; }
        public bool? LootShareEnabled { get; init; }
    }
}