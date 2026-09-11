using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Features
{
    public sealed record ContactPresenceOwner(uint MasterId, long SessionGeneration, string ConnectionId);

    public interface IContactsFeature
    {
        public IContactList<Friend> Friends { get; }
        public IContactList<Ignore> Ignores { get; }

        void ReplaceFriends(IEnumerable<Friend> friends, IEnumerable<ContactPresenceOwner> onlineOwners);
        Friend? TryApplySignIn(uint masterId, long sessionGeneration, string connectionId);
        Friend? TryApplySignOut(uint masterId, long sessionGeneration, string connectionId);
    }
}
