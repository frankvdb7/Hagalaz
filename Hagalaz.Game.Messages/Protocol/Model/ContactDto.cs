using Hagalaz.Game.Abstractions.Features.FriendsChat;

namespace Hagalaz.Game.Messages.Protocol.Model
{
    public record ContactDto
    {
        public required int MasterId { get; init; }
        public required string DisplayName { get; init; }
        public string? PreviousDisplayName { get; init; }
        public FriendsChatRank? Rank { get; init; }
        public string? WorldName { get; init; }
        public int? WorldId { get; init; }
    }
}
