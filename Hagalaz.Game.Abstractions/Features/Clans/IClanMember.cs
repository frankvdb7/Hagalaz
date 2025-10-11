namespace Hagalaz.Game.Abstractions.Features.Clans
{
    /// <summary>
    /// Defines the contract for a member of a clan, including their identity, rank, job, and current status.
    /// </summary>
    public interface IClanMember
    {
        /// <summary>
        /// Gets the unique identifier for the member's account.
        /// </summary>
        uint MasterId { get; }

        /// <summary>
        /// Gets the display name of the member.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Gets or sets the hierarchical rank of the member within the clan.
        /// </summary>
        ClanRank Rank { get; set; }

        /// <summary>
        /// Gets or sets the job title of the member within the clan.
        /// </summary>
        ClanJob Job { get; set; }

        /// <summary>
        /// Gets or sets the ID of the game world the member is currently on. A value of 0 typically indicates the member is offline or in the lobby.
        /// </summary>
        int WorldId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the member is currently in the game lobby rather than in the world.
        /// </summary>
        bool InLobby { get; set; }
    }
}
