using Hagalaz.Game.Abstractions.Features.Chat;
using Hagalaz.Game.Abstractions.Features.FriendsChat;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Represents a base class for a player contact, containing the master account ID.
    /// </summary>
    public abstract class Contact
    {
        /// <summary>
        /// Gets the unique identifier for the contact's master account.
        /// </summary>
        public required int MasterId { get; init; }
    }

    /// <summary>
    /// Represents a contact on a player's friends list.
    /// </summary>
    public class Friend : Contact
    {
        /// <summary>
        /// Gets or sets the rank of the friend within the player's friends chat.
        /// </summary>
        public required FriendsChatRank Rank { get; set; }

        /// <summary>
        /// Gets or sets the current availability status of the friend.
        /// </summary>
        public required Availability Availability { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the friendship is mutual (i.e., both players have added each other).
        /// </summary>
        public required bool AreMutualFriends { get; set; }
    }
    
    /// <summary>
    /// Represents a contact on a player's ignore list.
    /// </summary>
    public class Ignore : Contact {}
}