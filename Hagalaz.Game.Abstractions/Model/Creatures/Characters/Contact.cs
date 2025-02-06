using Hagalaz.Game.Abstractions.Features.Chat;
using Hagalaz.Game.Abstractions.Features.FriendsChat;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    public abstract class Contact
    {
        public required int MasterId { get; init; }
    }

    public class Friend : Contact
    {
        public required FriendsChatRank Rank { get; set; }
        public required Availability Availability { get; set; }
        public required bool AreMutualFriends { get; set; }
    }
    
    public class Ignore : Contact {}
}