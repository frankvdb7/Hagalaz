using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Nito.AsyncEx;

using Hagalaz.Services.GameWorld.Model;

namespace Hagalaz.Services.GameWorld.Store
{
    /// <summary>
    /// Stores local sessions while enforcing one account per process.
    /// Distributed ownership is enforced by <see cref="Services.IGameSessionClaimStore"/>.
    /// </summary>
    public class GameSessionStore : IGameSessionStore
    {
        private readonly AsyncReaderWriterLock _lock = new();
        private readonly Dictionary<string, IGameSession> _sessions = new();
        private readonly Dictionary<string, PendingWorldSession> _pendingWorldSessions = new();

        public async ValueTask<bool> TryAdd(IGameSession session)
        {
            using (await _lock.WriterLockAsync())
            {
                if (_sessions.ContainsKey(session.ConnectionId) ||
                    _sessions.Values.Any(existing => existing.MasterId == session.MasterId) ||
                    _pendingWorldSessions.Values.Any(existing => existing.Session.MasterId == session.MasterId))
                {
                    return false;
                }

                _sessions.Add(session.ConnectionId, session);
                return true;
            }
        }

        public async ValueTask<bool> TryReserveWorldSession(IGameWorldSession session)
        {
            using (await _lock.WriterLockAsync())
            {
                if (_pendingWorldSessions.ContainsKey(session.ConnectionId) ||
                    _sessions.ContainsKey(session.ConnectionId) &&
                    !_sessions[session.ConnectionId].MasterId.Equals(session.MasterId) ||
                    _sessions.Values.Any(existing => existing is IGameWorldSession worldSession &&
                                                     existing.MasterId == session.MasterId))
                {
                    return false;
                }

                var existingSession = _sessions.Values.FirstOrDefault(existing => existing.MasterId == session.MasterId);
                _pendingWorldSessions[session.ConnectionId] = new PendingWorldSession(session, existingSession);
                return true;
            }
        }

        public async ValueTask<(bool Committed, IGameSession? ReplacedSession)> TryCommitWorldSession(IGameWorldSession expectedSession)
        {
            using (await _lock.WriterLockAsync())
            {
                if (_sessions.TryGetValue(expectedSession.ConnectionId, out var activeSession) &&
                    ReferenceEquals(activeSession, expectedSession))
                {
                    return (true, null);
                }

                if (!_pendingWorldSessions.TryGetValue(expectedSession.ConnectionId, out var pendingSession) ||
                    !ReferenceEquals(pendingSession.Session, expectedSession))
                {
                    return (false, null);
                }

                var currentSession = _sessions.Values.FirstOrDefault(existing => existing.MasterId == expectedSession.MasterId);
                if (currentSession != null && !ReferenceEquals(currentSession, pendingSession.PreviousSession))
                {
                    return (false, null);
                }

                if (currentSession != null)
                {
                    _sessions.Remove(currentSession.ConnectionId);
                }

                _pendingWorldSessions.Remove(expectedSession.ConnectionId);
                _sessions[expectedSession.ConnectionId] = expectedSession;
                return (true, currentSession);
            }
        }

        public async ValueTask<bool> TryRemovePendingWorldSession(IGameSession expectedSession)
        {
            using (await _lock.WriterLockAsync())
            {
                return TryRemovePendingWorldSessionUnsafe(expectedSession);
            }
        }

        public async ValueTask<(bool Found, IGameSession? Session)> TryGetValue(string connectionId)
        {
            using (await _lock.ReaderLockAsync())
            {
                var found = _sessions.TryGetValue(connectionId, out var session);
                return (found, session);
            }
        }

        public async ValueTask<(bool Removed, IGameSession? Session)> TryRemove(string connectionId)
        {
            using (await _lock.WriterLockAsync())
            {
                var removed = _sessions.Remove(connectionId, out var session);
                return (removed, session);
            }
        }

        public async ValueTask<(bool Removed, IGameSession? Session)> TryRemove(IGameSession expectedSession)
        {
            using (await _lock.WriterLockAsync())
            {
                if (!_sessions.TryGetValue(expectedSession.ConnectionId, out var current) ||
                    !ReferenceEquals(current, expectedSession))
                {
                    return TryRemovePendingWorldSessionUnsafe(expectedSession)
                        ? (true, expectedSession)
                        : (false, null);
                }

                var removed = _sessions.Remove(expectedSession.ConnectionId, out var session);
                return (removed, session);
            }
        }

        public async ValueTask<bool> TryReplace(IGameSession expectedSession, IGameSession replacement)
        {
            using (await _lock.WriterLockAsync())
            {
                if (!_sessions.TryGetValue(expectedSession.ConnectionId, out var current) ||
                    !ReferenceEquals(current, expectedSession) ||
                    (_sessions.TryGetValue(replacement.ConnectionId, out var existingConnection) &&
                     !ReferenceEquals(existingConnection, expectedSession)) ||
                    _sessions.Values.Any(existing =>
                        !ReferenceEquals(existing, expectedSession) && existing.MasterId == replacement.MasterId))
                {
                    return false;
                }

                _sessions.Remove(expectedSession.ConnectionId);
                _sessions[replacement.ConnectionId] = replacement;
                return true;
            }
        }

        public async ValueTask<IGameSession?> FindByMasterId(uint masterId)
        {
            using (await _lock.ReaderLockAsync())
            {
                return _sessions.Values.FirstOrDefault(session => session.MasterId == masterId);
            }
        }

        public async ValueTask<IGameWorldSession?> FindWorldSessionByMasterId(uint masterId)
        {
            using (await _lock.ReaderLockAsync())
            {
                return _sessions.Values.OfType<IGameWorldSession>().FirstOrDefault(session =>
                    session.MasterId == masterId);
            }
        }

        public async ValueTask<IReadOnlyList<IGameSession>> FindAll()
        {
            using (await _lock.ReaderLockAsync())
            {
                return _sessions.Values.Concat(_pendingWorldSessions.Values.Select(pending => pending.Session)).ToArray();
            }
        }

        private bool TryRemovePendingWorldSessionUnsafe(IGameSession expectedSession)
        {
            if (!_pendingWorldSessions.TryGetValue(expectedSession.ConnectionId, out var pendingSession) ||
                !ReferenceEquals(pendingSession.Session, expectedSession))
            {
                return false;
            }

            return _pendingWorldSessions.Remove(expectedSession.ConnectionId);
        }

        private sealed record PendingWorldSession(IGameSession Session, IGameSession? PreviousSession);
    }
}
