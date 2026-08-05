using System;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Services.GameWorld.Factories;
using Hagalaz.Services.GameWorld.Model;
using Hagalaz.Services.GameWorld.Store;
using Hagalaz.Workers;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Services
{
    public class GameSessionService : IGameSessionService
    {
        private readonly IGameSessionStore _sessions;
        private readonly IGameSessionFactory _gameSessionFactory;
        private readonly IGameSessionClaimStore _claims;
        private readonly IGameSessionConnectionTerminator _connectionTerminator;
        private readonly ILogger<GameSessionService> _logger;
        private readonly GameSessionRetryQueue _retryQueue;

        public GameSessionService(
            IGameSessionStore sessions,
            IGameSessionFactory gameSessionFactory,
            IGameSessionClaimStore claims,
            IGameSessionConnectionTerminator connectionTerminator,
            ILogger<GameSessionService> logger,
            GameSessionRetryQueue retryQueue)
        {
            _sessions = sessions;
            _gameSessionFactory = gameSessionFactory;
            _claims = claims;
            _connectionTerminator = connectionTerminator;
            _logger = logger;
            _retryQueue = retryQueue;
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
                        retainPendingForCleanup = !QueueClaimRelease(masterId, createdSession.SessionClaimId);
                    }
                }
                catch (Exception releaseException)
                {
                    _logger.LogWarning(releaseException,
                        "Failed to release world-session claim '{sessionClaimId}' after claim acquisition failed for account '{masterId}'; queuing exact-owner cleanup.",
                        createdSession.SessionClaimId,
                        masterId);
                    retainPendingForCleanup = !QueueClaimRelease(masterId, createdSession.SessionClaimId);
                }

                throw;
            }
            finally
            {
                if (!claimAcquired && retainPendingForCleanup)
                {
                    await _sessions.TryRetainWorldSessionForCleanup(createdSession);
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
                try
                {
                    _connectionTerminator.Abort(replacedSession);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to abort replaced game session '{connectionId}' after promoting '{promotedConnectionId}'.",
                        replacedSession.ConnectionId,
                        expectedSession.ConnectionId);
                    if (!_retryQueue.TryQueueConnectionAbort(replacedSession))
                    {
                        _logger.LogCritical(
                            "Could not enqueue the failed abort for replaced game session '{connectionId}' after promoting '{promotedConnectionId}'.",
                            replacedSession.ConnectionId,
                            expectedSession.ConnectionId);
                    }
                }
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

                var cleanup = await ReleaseClaimOrQueueAsync(
                    worldSession.MasterId,
                    worldSession.SessionClaimId,
                    cancellationToken,
                    "during pending-session cleanup",
                    expectedSession);
                if (cleanup == ClaimCleanupResult.Deferred)
                {
                    await _sessions.TryRetainWorldSessionForCleanup(expectedSession);
                    return false;
                }

                var removedPending = await _sessions.TryRemovePendingWorldSession(expectedSession);
                return removedPending;
            }

            if (storedSession.Session is IGameWorldSession storedWorldSession)
            {
                var cleanup = await ReleaseClaimOrQueueAsync(
                    storedWorldSession.MasterId,
                    storedWorldSession.SessionClaimId,
                    cancellationToken,
                    "during session cleanup",
                    expectedSession);
                if (cleanup == ClaimCleanupResult.Deferred)
                {
                    await _sessions.TryRetainWorldSessionForCleanup(expectedSession);
                    return false;
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

        private async Task<ClaimCleanupResult> ReleaseClaimOrQueueAsync(
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
                return QueueClaimRelease(masterId, claimId)
                    ? ClaimCleanupResult.Queued
                    : ClaimCleanupResult.Deferred;
            }
            catch (OperationCanceledException)
            {
                if (!QueueClaimRelease(masterId, claimId))
                {
                    await _sessions.TryRetainWorldSessionForCleanup(expectedSession);
                }

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to release world-session claim '{sessionClaimId}' for account '{masterId}' {operation}; retaining exact-owner cleanup for retry.",
                    claimId,
                    masterId,
                    operation);
                return QueueClaimRelease(masterId, claimId)
                    ? ClaimCleanupResult.Queued
                    : ClaimCleanupResult.Deferred;
            }
        }

        private async Task<bool> CleanupFailedWorldCommitAsync(IGameWorldSession worldSession)
        {
            var cleanup = await ReleaseClaimOrQueueAsync(
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
                await _sessions.TryRetainWorldSessionForCleanup(worldSession);
                return false;
            }

            await _sessions.TryRemovePendingWorldSession(worldSession);
            return false;
        }

        private bool QueueClaimRelease(uint masterId, string claimId)
        {
            var queued = _retryQueue.TryQueueClaimRelease(masterId, claimId);
            if (!queued)
            {
                _logger.LogCritical(
                    "Could not enqueue exact-owner cleanup for world-session claim '{sessionClaimId}' for account '{masterId}'; the local session will be retained for independent cleanup reconciliation.",
                    claimId,
                    masterId);
            }

            return queued;
        }

        private enum ClaimCleanupResult
        {
            Released,
            Queued,
            Deferred
        }
    }
}
