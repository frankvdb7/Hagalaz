namespace Hagalaz.Game.Abstractions.Features.Clans
{
    /// <summary>
    /// 
    /// </summary>
    public interface IClanMember
    {
        /// <summary>
        /// The member's associated master id.
        /// </summary>
        uint MasterId { get; }
        /// <summary>
        /// The member's name.
        /// </summary>
        string DisplayName { get; }
        /// <summary>
        /// The priviledges this member has in a clan.
        /// </summary>
        ClanRank Rank { get; set; }
        /// <summary>
        /// The job this member has in a clan.
        /// </summary>
        ClanJob Job { get; set; }
        /// <summary>
        /// The member's current world.
        /// </summary>
        int WorldId { get; set; }
        /// <summary>
        /// Whether the member is in the lobby.
        /// </summary>
        bool InLobby { get; set; }
    }
}
