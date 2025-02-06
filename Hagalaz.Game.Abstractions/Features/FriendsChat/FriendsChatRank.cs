namespace Hagalaz.Game.Abstractions.Features.FriendsChat
{
    /// <summary>
    /// Defines the ranks availible in a clan chat room.
    /// </summary>
    public enum FriendsChatRank : sbyte
    {
        /// <summary>
        /// Banned user, who can't join chat channel.
        /// </summary>
        Banned = -3,
        /// <summary>
        /// Used only on share loot, this means no one can share
        /// </summary>
        NoOne = -2,
        /// <summary>
        /// A regular player within the chat channel. This player has no rights.
        /// </summary>
        Regular = -1,
        /// <summary>
        /// A player who is friend of the chat channel owner.
        /// </summary>
        Friend = 0,
        /// <summary>
        /// A recruit of the chat channel.
        /// </summary>
        Recruit = 1,
        /// <summary>
        /// A corperal of the chat channel.
        /// </summary>
        Corporal = 2,
        /// <summary>
        /// A sergeant of the chat channel.
        /// </summary>
        Sergeant = 3,
        /// <summary>
        /// A lieutenant of the chat channel.
        /// </summary>
        Lieutenant = 4,
        /// <summary>
        /// A captain of the chat channel.
        /// </summary>
        Captain = 5,
        /// <summary>
        /// A general of the chat channel.
        /// </summary>
        General = 6,
        /// <summary>
        /// The owner of the chat channel.
        /// </summary>
        Owner = 7,
        /// <summary>
        /// An administrator with full rights in any chat.
        /// </summary>
        Admin = 127
    }
}
