namespace Hagalaz.Game.Abstractions.Features.Clans
{
    /// <summary>
    /// Defines the hierarchical ranks within a clan, which determine a member's permissions and standing.
    /// </summary>
    public enum ClanRank : sbyte
    {
        /// <summary>
        /// Banned user, who can't join clan.
        /// </summary>
        Banned = -2,
        /// <summary>
        /// Guest user, has not officially joined the clan, 
        /// but can read and write messages in the chat channel.
        /// </summary>
        Guest = -1,
        /// <summary>
        /// A recruit of the clan.
        /// </summary>
        Recruit = 0,
        /// <summary>
        /// A corporal of the clan.
        /// </summary>
        Corporal = 1,
        /// <summary>
        /// A sergeant of the clan.
        /// </summary>
        Sergeant = 2,
        /// <summary>
        /// A lieutenant of the clan.
        /// </summary>
        Lieutenant = 3,
        /// <summary>
        /// A captain of the clan.
        /// </summary>
        Captain = 4,
        /// <summary>
        /// A general of the clan.
        /// </summary>
        General = 5,
        /// <summary>
        /// The admin of the clan.
        /// </summary>
        Admin = 100,
        /// <summary>
        /// The organiser of the clan.
        /// </summary>
        Organiser = 122,
        /// <summary>
        /// The coordinator of the clan.
        /// </summary>
        Coordinator = 123,
        /// <summary>
        /// The overseer of the clan.
        /// </summary>
        Overseer = 124,
        /// <summary>
        /// The deputy owner of the clan.
        /// </summary>
        DeputyOwner = 125,
        /// <summary>
        /// The owner of the clan.
        /// </summary>
        Owner = 126
    }
}
