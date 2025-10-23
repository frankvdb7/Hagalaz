using Hagalaz.Game.Abstractions.Features.Chat;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Extensions
{
    /// <summary>
    /// Provides extension methods for contact-related classes, such as <see cref="Friend"/>.
    /// </summary>
    public static class ContactExtensions
    {
        /// <summary>
        /// Gets the world ID of a friend, taking into account their privacy settings and mutual friend status.
        /// </summary>
        /// <param name="friend">The friend whose world ID is being requested.</param>
        /// <param name="worldId">The actual world ID of the friend.</param>
        /// <returns>
        /// The world ID if the friend is visible; otherwise, <c>null</c>.
        /// </returns>
        public static int? GetWorldId(this Friend friend, int? worldId)
        {
            if (friend.Availability == Availability.Nobody)
            {
                return null;
            }

            if (friend.Availability == Availability.Friends && !friend.AreMutualFriends)
            {
                return null;
            }

            return worldId;
        }
    }
}
