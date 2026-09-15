using System;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Services.GameWorld.Factories;
using Hagalaz.Services.GameWorld.Model;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Services
{
    public class GameSessionService : IGameSessionService
    {
        private readonly IGameSessionStore _sessions;
        private readonly IGameSessionFactory _gameSessionFactory;
        private readonly IGameSessionClaimStore _claims;
        private readonly ILogger<GameSessionService> _logger;
        private readonly GameSessionAbortCoordinator _abortCoordinator;

        public GameSessionService(
            IGameSessionStore sessions,
            IGameSessionFactory gameSessionFactory,
            IGameSessionClaimStore claims,
            ILogger<GameSessionService> logger,
            GameSessionAbortCoordinator abortCoordinator)
        {
            _sessions = sessions;
            _gameSessionFactory = gameSessionFactory;
            _claims = claims;
            _logger = logger;
            _abortCoordinator = abortCoordinator;
        }

        public async Task<(IGameSession Session, bool Created)> AddSession(uint masterId, string connectionId)
        {
            var existingSession = await _sessions.TryGetValue(connectionId);
            if (existingSession.Found)
            {
                return (existingSession.Session!, Created: false);
            }

            var sessionGeneration = await _claims.AllocateSessionGenerationAsync(masterId);
            var createdSession = _gameSessionFactory.Create(masterId, connectionId, sessionGeneration);
            try
            {
                if (!await _claims.TryClaimAsync(masterId, createdSession.SessionClaimId))
                {
                    return (await _sessions.FindByMasterId(masterId) ?? createdSession, Created: false);
                }

                if (!await _sessions.TryAdd(createdSession))
                {
                    await ReleaseClaimAfterAdmissionFailureAsync(createdSession);
                    return (await _sessions.FindByMasterId(masterId) ?? createdSession, Created: false);
                }

                return (createdSession, Created: true);
            }
            catch (Exception)
            {
                await ReleaseClaimAfterAdmissionFailureAsync(createdSession);
                throw;
            }
        }

        public Task<(IGameSession? Session, bool Created)> TryAddWorldSession(
            uint masterId,
            string connectionId,
            CancellationToken cancellationToken = default) =>
            TryAddWorldSession(
                masterId,
                connectionId,
                lobbySessionClaimId: null,
                cancellationToken: cancellationToken);

        public async Task<(IGameSession? Session, bool Created)> TryAddWorldSession(
            uint masterId,
            string connectionId,
            string? lobbySessionClaimId,
            CancellationToken cancellationToken = default)
        {
            var existingSession = await _sessions.FindByMasterId(masterId);
            if (existingSession is IGameWorldSession)
            {
                return (null, false);
            }

            var sessionGeneration = await _claims.AllocateSessionGenerationAsync(masterId, cancellationToken);
            var createdSession = _gameSessionFactory.CreateWorld(masterId, connectionId, sessionGeneration);
            if (!await _sessions.TryReserveWorldSession(createdSession, lobbySessionClaimId))
            {
                return (await _sessions.FindWorldSessionByMasterId(masterId), false);
            }

            if (await _sessions.FindPendingWorldSessionPreviousClaimId(createdSession) != null)
            {
                // The existing lobby claim remains authoritative while world
                // initialization is pending. CommitWorldSession transfers it
                // atomically with the local session replacement.
                return (createdSession, true);
            }

            if (lobbySessionClaimId != null)
            {
                // A remote lobby owner cannot be observed in this process. The
                // exact claim transfer is deferred until commit, while the
                // distributed claim store proves that this handoff is current.
                return (createdSession, true);
            }

            try
            {
                if (await _claims.TryClaimAsync(masterId, createdSession.SessionClaimId, cancellationToken))
                {
                    return (createdSession, true);
                }
            }
            catch (Exception)
            {
                await RetainSessionForCleanupAsync(createdSession, "after claim acquisition failed");
                throw;
            }

            await _sessions.TryRemovePendingWorldSession(createdSession);
            return (await _sessions.FindWorldSessionByMasterId(masterId), false);
        }

        public async Task<bool> CommitWorldSession(
            IGameSession expectedSession,
            CancellationToken cancellationToken = default)
        {
            if (expectedSession is not IGameWorldSession worldSession)
            {
                return false;
            }

            IGameSession? replacedSession = null;
            var previousClaimId = await _sessions.FindPendingWorldSessionPreviousClaimId(worldSession);
            var commit = new Func<CancellationToken, Task<bool>>(async _ =>
            {
                var result = await _sessions.TryCommitWorldSession(worldSession);
                replacedSession = result.ReplacedSession;
                return result.Committed;
            });
            bool committed;
            try
            {
                committed = previousClaimId != null
                    ? await _claims.ExecuteIfOwnerAndReplaceAsync(
                        expectedSession.MasterId,
                        previousClaimId,
                        worldSession.SessionClaimId,
                        commit,
                        cancellationToken)
                    : await _claims.ExecuteIfOwnerAsync(
                        expectedSession.MasterId,
                        worldSession.SessionClaimId,
                        commit,
                        cancellationToken);
            }
            catch (Exception)
            {
                await RetainSessionForCleanupAsync(worldSession, "after claim transfer failed");
                throw;
            }
            if (!committed)
            {
                if (previousClaimId != null)
                {
                    await _sessions.TryRemovePendingWorldSession(worldSession);
                    return false;
                }

                if (!await TryReleaseClaimAsync(
                    worldSession.MasterId,
                    worldSession.SessionClaimId,
                    CancellationToken.None,
                    "after commit failed",
                    worldSession))
                {
                    await RetainSessionForCleanupAsync(worldSession, "after commit failed");
                    return false;
                }

                await _sessions.TryRemovePendingWorldSession(worldSession);
                return false;
            }

            if (replacedSession != null &&
                !ReferenceEquals(replacedSession, expectedSession) &&
                replacedSession.ConnectionId != expectedSession.ConnectionId)
            {
                // Promotion is already committed. Cleanup must not turn request
                // cancellation into a failed world sign-in.
                await _abortCoordinator.AbortPendingSessionAsync(replacedSession, CancellationToken.None);
            }

            return true;
        }

        public async Task<bool> RemoveSession(IGameSession expectedSession, CancellationToken cancellationToken = default)
        {
            var storedSession = await _sessions.TryGetValue(expectedSession.ConnectionId);
            if (storedSession.Found &&
                storedSession.Session != null &&
                !ReferenceEquals(storedSession.Session, expectedSession))
            {
                return false;
            }

            if (!storedSession.Found ||
                storedSession.Session == null)
            {
                if (!storedSession.Found && !await _sessions.IsPendingWorldSession(expectedSession))
                {
                    return false;
                }

                if (expectedSession is not IGameWorldSession worldSession)
                {
                    return await _sessions.TryRemovePendingWorldSession(expectedSession);
                }

                if (await _sessions.FindPendingWorldSessionPreviousClaimId(worldSession) != null)
                {
                    return await _sessions.TryRemovePendingWorldSession(worldSession);
                }

                if (!await TryReleaseClaimAsync(
                    worldSession.MasterId,
                    worldSession.SessionClaimId,
                    cancellationToken,
                    "during pending-session cleanup",
                    expectedSession))
                {
                    await RetainSessionForCleanupAsync(expectedSession, "during pending-session cleanup");
                    return false;
                }

                var removedPending = await _sessions.TryRemovePendingWorldSession(expectedSession);
                return removedPending;
            }

            if (storedSession.Session is IGameWorldSession storedWorldSession)
            {
                if (!await TryReleaseClaimAsync(
                    storedWorldSession.MasterId,
                    storedWorldSession.SessionClaimId,
                    cancellationToken,
                    "during session cleanup",
                    expectedSession))
                {
                    // The active session is logically logged out even when the
                    // distributed claim must be retried by the lease worker.
                    return await RetainSessionForCleanupAsync(expectedSession, "during session cleanup");
                }

                var removedStoredSession = await _sessions.TryRemove(expectedSession);
                return removedStoredSession.Removed;
            }

            if (!await TryReleaseClaimAsync(
                storedSession.Session.MasterId,
                storedSession.Session.SessionClaimId,
                cancellationToken,
                "during session cleanup",
                expectedSession))
            {
                return await RetainSessionForCleanupAsync(expectedSession, "during session cleanup");
            }

            var removedSession = await _sessions.TryRemove(expectedSession);
            return removedSession.Removed;
        }

        private async Task ReleaseClaimAfterAdmissionFailureAsync(IGameSession session)
        {
            try
            {
                if (await _claims.ReleaseAsync(session.MasterId, session.SessionClaimId))
                {
                    return;
                }

                _logger.LogWarning(
                    "Session claim '{sessionClaimId}' was no longer owned after local admission failed for account '{masterId}'.",
                    session.SessionClaimId,
                    session.MasterId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to release session claim '{sessionClaimId}' after local admission failed for account '{masterId}'; retaining exact-owner cleanup.",
                    session.SessionClaimId,
                    session.MasterId);
                if (!await _sessions.TryAddPendingClaimCleanup(session))
                {
                    _logger.LogCritical(
                        "Could not retain session claim '{sessionClaimId}' for reconciliation after local admission failed for account '{masterId}'.",
                        session.SessionClaimId,
                        session.MasterId);
                }
            }
        }

        public async Task<IGameSession?> FindByMasterId(uint masterId) => await _sessions.FindByMasterId(masterId);

        public async Task<IGameWorldSession?> FindWorldSessionByMasterId(uint masterId) =>
            await _sessions.FindWorldSessionByMasterId(masterId);

        private async Task<bool> TryReleaseClaimAsync(
            uint masterId,
            string claimId,
            CancellationToken cancellationToken,
            string operation,
            IGameSession expectedSession)
        {
            try
            {
                if (await _claims.ReleaseAsync(masterId, claimId, cancellationToken))
                {
                    return true;
                }

                _logger.LogWarning(
                    "Session claim '{sessionClaimId}' for account '{masterId}' was no longer current {operation}; cleanup is resolved.",
                    claimId,
                    masterId,
                    operation);
                return true;
            }
            catch (OperationCanceledException)
            {
                await RetainSessionForCleanupAsync(expectedSession, "after claim release cancellation");
                throw;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex,
                    "Failed to release session claim '{sessionClaimId}' for account '{masterId}' {operation}; retaining exact-owner cleanup for retry.",
                    claimId,
                    masterId,
                    operation);
                return false;
            }
        }

        private async Task<bool> RetainSessionForCleanupAsync(IGameSession expectedSession, string operation)
        {
            var retained = await _sessions.TryMoveToPendingClaimCleanup(expectedSession);
            if (!retained)
            {
                _logger.LogCritical(
                    "Could not retain session for cleanup '{connectionId}' {operation}; no local reconciliation record exists.",
                    expectedSession.ConnectionId,
                    operation);
            }

            return retained;
        }

    }
}
