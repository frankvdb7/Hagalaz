using Hagalaz.Game.Abstractions.Features.FriendsChat;

namespace Hagalaz.Game.Network.Model
{
    /// <summary>
    /// Represents a single member inside a friends chat channel.
    /// </summary>
    public record FriendsChatMemberDto
    {
        /// <summary>
        /// The member's name.
        /// </summary>
        public required string DisplayName { get; init; }
        /// <summary>
        /// The member's previous name.
        /// </summary>
        public string? PreviousDisplayName { get; init; }
        /// <summary>
        /// The priviledges this member has in a chat channel.
        /// </summary>
        public FriendsChatRank Rank { get; init; }

        /// <summary>
        /// The member's current world.
        /// </summary>
        public int WorldId { get; init; }
        /// <summary>
        /// Whether the member is in the lobby.
        /// </summary>
        public bool InLobby { get; init; }
    }
}
