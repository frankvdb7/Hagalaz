using Hagalaz.Game.Abstractions.Features.Chat;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Extensions
{
    public static class ContactExtensions
    {
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
