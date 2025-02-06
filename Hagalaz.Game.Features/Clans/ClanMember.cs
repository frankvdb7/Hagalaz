using Hagalaz.Game.Abstractions.Features.Clans;

namespace Hagalaz.Game.Features.Clans
{
    /// <summary>
    /// Represents a single member inside a clan.
    /// </summary>
    public class ClanMember : IClanMember
    {
        /// <summary>
        /// The member's associated master id.
        /// </summary>
        public uint MasterId { get; set; }
        /// <summary>
        /// The member's name.
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// The privileges this member has in a clan.
        /// </summary>
        public ClanRank Rank { get; set; } = ClanRank.Recruit;
        /// <summary>
        /// The job this member has in a clan.
        /// </summary>
        public ClanJob Job { get; set; } = ClanJob.None;
        /// <summary>
        /// The member's current world.
        /// </summary>
        public int WorldId { get; set; }
        /// <summary>
        /// Whether the member is in the lobby.
        /// </summary>
        public bool InLobby { get; set; }
    }
}
