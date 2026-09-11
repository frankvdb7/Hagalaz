using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Features
{
    public class WorldContactsFeature : IContactsFeature
    {
        private readonly ICharacter _character;
        private readonly object _presenceGate = new();
        private readonly System.Collections.Generic.Dictionary<uint, SessionIdentity> _presence = new();

        public WorldContactsFeature(ICharacter character) => _character = character;

        public IContactList<Friend> Friends => _character.Friends;

        public IContactList<Ignore> Ignores => _character.Ignores;

        public Friend? TryApplySignIn(uint masterId, long sessionGeneration, string connectionId)
        {
            lock (_presenceGate)
            {
                if (_presence.TryGetValue(masterId, out var current))
                {
                    if (sessionGeneration <= current.SessionGeneration)
                    {
                        return null;
                    }
                }

                var friend = Friends.Get(masterId);
                if (friend is null)
                {
                    return null;
                }

                _presence[masterId] = new SessionIdentity(sessionGeneration, connectionId);
                return friend;
            }
        }

        public Friend? TryApplySignOut(uint masterId, long sessionGeneration, string connectionId)
        {
            lock (_presenceGate)
            {
                if (_presence.TryGetValue(masterId, out var current) &&
                    (current.SessionGeneration != sessionGeneration || current.ConnectionId != connectionId))
                {
                    return null;
                }

                var friend = Friends.Get(masterId);
                if (friend is null)
                {
                    return null;
                }

                _presence.Remove(masterId);
                return friend;
            }
        }

        private readonly record struct SessionIdentity(long SessionGeneration, string ConnectionId);
    }
}
