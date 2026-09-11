using System;
using System.Collections.Generic;

namespace Hagalaz.Services.GameWorld.Features
{
    internal sealed class ContactPresenceState
    {
        private readonly object _gate = new();
        private readonly Dictionary<uint, SessionIdentity> _owners = new();

        public bool TrySignIn(
            uint masterId,
            long sessionGeneration,
            string connectionId,
            Func<bool> contactExists)
        {
            lock (_gate)
            {
                if (_owners.TryGetValue(masterId, out var current) &&
                    sessionGeneration <= current.SessionGeneration)
                {
                    return false;
                }

                if (!contactExists())
                {
                    return false;
                }

                _owners[masterId] = new SessionIdentity(sessionGeneration, connectionId);
                return true;
            }
        }

        public bool TrySignOut(
            uint masterId,
            long sessionGeneration,
            string connectionId,
            Func<bool> contactExists)
        {
            lock (_gate)
            {
                if (_owners.TryGetValue(masterId, out var current) &&
                    (current.SessionGeneration != sessionGeneration || current.ConnectionId != connectionId))
                {
                    return false;
                }

                if (!contactExists())
                {
                    return false;
                }

                _owners.Remove(masterId);
                return true;
            }
        }

        private readonly record struct SessionIdentity(long SessionGeneration, string ConnectionId);
    }
}
