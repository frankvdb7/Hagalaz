using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Features
{
    public sealed record ContactPresenceOwner(
        uint MasterId,
        long SessionGeneration,
        string ConnectionId);

    public interface IContactsFeature
    {
        public IContactList<Friend> Friends { get; }
        public IContactList<Ignore> Ignores { get; }

        long CaptureObservationBoundary();
        long BeginObservationWindow();
        void EndObservationWindow();
        Task WaitForInitialSnapshotAsync(CancellationToken cancellationToken = default);
        void ReplaceFriends(IEnumerable<Friend> friends, IEnumerable<ContactPresenceOwner> onlineOwners, long observationBoundary = 0);
        Friend AddFriend(Friend friend, ContactPresenceOwner? onlineOwner, long observationBoundary = 0);
        bool RemoveFriend(uint masterId);
        Friend? TryApplySignIn(uint masterId, long sessionGeneration, string connectionId);
        Friend? TryApplySignOut(uint masterId, long sessionGeneration, string connectionId);
    }
}
