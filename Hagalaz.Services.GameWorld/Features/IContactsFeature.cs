using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Features
{
    public readonly record struct ContactPresenceView(bool IsOnline, int? WorldId, string? WorldName);

    public sealed record ContactPresenceOwner(
        uint MasterId,
        long SessionGeneration,
        string ConnectionId,
        int? WorldId = null,
        string? WorldName = null);

    public interface IContactsFeature
    {
        public IContactList<Friend> Friends { get; }
        public IContactList<Ignore> Ignores { get; }

        long CaptureObservationBoundary();
        long BeginObservationWindow();
        void EndObservationWindow();
        Task WaitForInitialSnapshotAsync(CancellationToken cancellationToken = default);
        void CompleteInitialSnapshot();
        ContactPresenceView GetPresence(uint masterId);
        void ReplaceFriends(IEnumerable<Friend> friends, IEnumerable<ContactPresenceOwner> onlineOwners, long observationBoundary = 0);
        Friend AddFriend(Friend friend, ContactPresenceOwner? onlineOwner, long observationBoundary = 0);
        bool RemoveFriend(uint masterId);
        Friend? TryApplySignIn(uint masterId, long sessionGeneration, string connectionId, int? worldId = null, string? worldName = null);
        Friend? TryApplySignOut(uint masterId, long sessionGeneration, string connectionId);
    }
}
