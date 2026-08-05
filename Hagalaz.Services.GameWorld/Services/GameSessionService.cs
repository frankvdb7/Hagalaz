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

            var createdSession = _gameSessionFactory.Create(masterId, connectionId);
            if (!await _sessions.TryAdd(createdSession))
            {
                return (await _sessions.FindByMasterId(masterId) ?? createdSession, Created: false);
            }

            return (createdSession, Created: true);
        }

        public async Task<(IGameSession? Session, bool Created)> TryAddWorldSession(
            uint masterId,
            string connectionId,
            CancellationToken cancellationToken = default)
        {
            var existingSession = await _sessions.FindWorldSessionByMasterId(masterId);
            if (existingSession != null)
            {
                return (null, false);
            }

            var createdSession = _gameSessionFactory.CreateWorld(masterId, connectionId);
            if (!await _sessions.TryReserveWorldSession(createdSession))
            {
                return (await _sessions.FindWorldSessionByMasterId(masterId), false);
            }

            var claimAcquired = false;
            var retainPendingForCleanup = false;
            try
            {
                claimAcquired = await _claims.TryClaimAsync(masterId, createdSession.SessionClaimId, cancellationToken);
                return claimAcquired
                    ? (createdSession, true)
                    : (await _sessions.FindWorldSessionByMasterId(masterId), false);
            }
            catch
            {
                try
                {
                    // TryClaimAsync may have persisted the claim before an infrastructure
                    // exception escaped (for example while releasing its distributed lock).
                    // The value check in ReleaseAsync prevents this cleanup from removing a
                    // claim acquired by another owner.
                    if (!await _claims.ReleaseAsync(masterId, createdSession.SessionClaimId, CancellationToken.None))
                    {
                        retainPendingForCleanup = true;
                    }
                }
                catch (Exception releaseException)
                {
                    _logger.LogWarning(releaseException,
                        "Failed to release world-session claim '{sessionClaimId}' after claim acquisition failed for account '{masterId}'; retaining it for lease reconciliation.",
                        createdSession.SessionClaimId,
                        masterId);
                    retainPendingForCleanup = true;
                }

                throw;
            }
            finally
            {
                if (!claimAcquired && retainPendingForCleanup)
                {
                    await RetainWorldSessionForCleanupAsync(createdSession, "after claim acquisition failed");
                }
                else if (!claimAcquired)
                {
                    await _sessions.TryRemovePendingWorldSession(createdSession);
                }
            }
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
            var committed = await _claims.ExecuteIfOwnerAsync(
                expectedSession.MasterId,
                worldSession.SessionClaimId,
                async _ =>
                {
                    var result = await _sessions.TryCommitWorldSession(worldSession);
                    replacedSession = result.ReplacedSession;
                    return result.Committed;
                },
                cancellationToken);
            if (!committed)
            {
                return await CleanupFailedWorldCommitAsync(worldSession);
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

                var cleanup = await ReleaseClaimAsync(
                    worldSession.MasterId,
                    worldSession.SessionClaimId,
                    cancellationToken,
                    "during pending-session cleanup",
                    expectedSession);
                if (cleanup == ClaimCleanupResult.Deferred)
                {
                    await RetainWorldSessionForCleanupAsync(expectedSession, "during pending-session cleanup");
                    return false;
                }

                var removedPending = await _sessions.TryRemovePendingWorldSession(expectedSession);
                return removedPending;
            }

            if (storedSession.Session is IGameWorldSession storedWorldSession)
            {
                var cleanup = await ReleaseClaimAsync(
                    storedWorldSession.MasterId,
                    storedWorldSession.SessionClaimId,
                    cancellationToken,
                    "during session cleanup",
                    expectedSession);
                if (cleanup == ClaimCleanupResult.Deferred)
                {
                    // The active session is logically logged out even when the
                    // distributed claim must be retried by the lease worker.
                    return await RetainWorldSessionForCleanupAsync(expectedSession, "during session cleanup");
                }

                var removedStoredSession = await _sessions.TryRemove(expectedSession);
                return removedStoredSession.Removed;
            }

            var removedSession = await _sessions.TryRemove(expectedSession);
            return removedSession.Removed;
        }

        public async Task<bool> RemoveLocalSession(IGameSession expectedSession)
        {
            var removedSession = await _sessions.TryRemove(expectedSession);
            return removedSession.Removed;
        }

        public async Task<IGameSession?> FindByMasterId(uint masterId) => await _sessions.FindByMasterId(masterId);

        private async Task<ClaimCleanupResult> ReleaseClaimAsync(
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
                    return ClaimCleanupResult.Released;
                }

                _logger.LogWarning(
                    "World-session claim '{sessionClaimId}' for account '{masterId}' was not released {operation}; retaining exact-owner cleanup for retry.",
                    claimId,
                    masterId,
                    operation);
                return ClaimCleanupResult.Deferred;
            }
            catch (OperationCanceledException)
            {
                await RetainWorldSessionForCleanupAsync(expectedSession, "after claim release cancellation");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to release world-session claim '{sessionClaimId}' for account '{masterId}' {operation}; retaining exact-owner cleanup for retry.",
                    claimId,
                    masterId,
                    operation);
                return ClaimCleanupResult.Deferred;
            }
        }

        private async Task<bool> CleanupFailedWorldCommitAsync(IGameWorldSession worldSession)
        {
            var cleanup = await ReleaseClaimAsync(
                worldSession.MasterId,
                worldSession.SessionClaimId,
                CancellationToken.None,
                "after commit failed",
                worldSession);
            if (cleanup == ClaimCleanupResult.Deferred)
            {
                _logger.LogWarning(
                    "World-session claim '{sessionClaimId}' was not released after commit failed for account '{masterId}'; retaining the pending reservation for independent cleanup reconciliation.",
                    worldSession.SessionClaimId,
                    worldSession.MasterId);
                await RetainWorldSessionForCleanupAsync(worldSession, "after commit failed");
                return false;
            }

            await _sessions.TryRemovePendingWorldSession(worldSession);
            return false;
        }

        private async Task<bool> RetainWorldSessionForCleanupAsync(IGameSession expectedSession, string operation)
        {
            var retained = await _sessions.TryRetainWorldSessionForCleanup(expectedSession);
            if (!retained)
            {
                _logger.LogCritical(
                    "Could not retain world session for cleanup '{connectionId}' {operation}; no local reconciliation record exists.",
                    expectedSession.ConnectionId,
                    operation);
            }

            return retained;
        }

        private enum ClaimCleanupResult
        {
            Released,
            Deferred
        }
    }
}
