using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Services.GameWorld.Model;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Services;

public interface IGameSessionConnectionTerminator
{
    void Abort(IGameSession session);
}

public sealed class GameSessionConnectionTerminator : IGameSessionConnectionTerminator
{
    private readonly Raido.Server.RaidoConnectionStore _connections;

    public GameSessionConnectionTerminator(Raido.Server.RaidoConnectionStore connections) => _connections = connections;

    public void Abort(IGameSession session) => _connections[session.ConnectionId]?.Abort();
}

public sealed class GameSessionLeaseService : BackgroundService
{
    private readonly IGameSessionStore _sessions;
    private readonly IGameSessionClaimStore _claims;
    private readonly IGameSessionConnectionTerminator _connectionTerminator;
    private readonly ILogger<GameSessionLeaseService> _logger;
    private readonly GameSessionRetryQueue _retryQueue;

    public GameSessionLeaseService(
        IGameSessionStore sessions,
        IGameSessionClaimStore claims,
        IGameSessionConnectionTerminator connectionTerminator,
        ILogger<GameSessionLeaseService> logger,
        GameSessionRetryQueue retryQueue)
    {
        _sessions = sessions;
        _claims = claims;
        _connectionTerminator = connectionTerminator;
        _logger = logger;
        _retryQueue = retryQueue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(GameSessionClaimOptions.RenewalInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RenewSessionsAsync(stoppingToken);
        }
    }

    internal async Task RenewSessionsAsync(CancellationToken cancellationToken)
    {
        var pendingCleanupSessions = await _sessions.FindWorldSessionsPendingCleanup();
        var deferredAbortSessions = await _sessions.FindSessionsPendingAbort();
        var pendingCleanupSet = pendingCleanupSessions.Count == 0
            ? null
            : new HashSet<IGameWorldSession>(pendingCleanupSessions, ReferenceEqualityComparer.Instance);
        foreach (var session in await _sessions.FindAll())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (session is not IGameWorldSession worldSession ||
                pendingCleanupSet?.Contains(worldSession) == true)
            {
                continue;
            }

            try
            {
                if (await _claims.RenewAsync(worldSession.MasterId, worldSession.SessionClaimId, cancellationToken))
                {
                    continue;
                }

                _logger.LogWarning("Lost active game-session claim for account '{masterId}' and session '{sessionClaimId}'. Aborting the connection.",
                    worldSession.MasterId, worldSession.SessionClaimId);
                await AbortAndReconcileLostSession(worldSession);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to renew active game-session claim for account '{masterId}'. Aborting the connection.",
                    worldSession.MasterId);
                await AbortAndReconcileLostSession(worldSession);
            }
        }

        await ReconcileDeferredClaimReleasesAsync(pendingCleanupSessions, cancellationToken);
        await ReconcileDeferredConnectionAbortsAsync(deferredAbortSessions, cancellationToken);
    }

    private async Task ReconcileDeferredClaimReleasesAsync(
        IReadOnlyList<IGameWorldSession> pendingSessions,
        CancellationToken cancellationToken)
    {
        foreach (var pendingSession in pendingSessions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (await _claims.ReleaseAsync(
                        pendingSession.MasterId,
                        pendingSession.SessionClaimId,
                        cancellationToken))
                {
                    await _sessions.TryRemovePendingWorldSession(pendingSession);
                    _logger.LogInformation(
                        "Released deferred world-session claim '{sessionClaimId}' for account '{masterId}'.",
                        pendingSession.SessionClaimId,
                        pendingSession.MasterId);
                }
                else
                {
                    // A false result proves that this exact owner no longer exists.
                    await _sessions.TryRemovePendingWorldSession(pendingSession);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex,
                    "Failed to reconcile deferred world-session claim '{sessionClaimId}' for account '{masterId}'; it will be retried on the next lease cycle.",
                    pendingSession.SessionClaimId,
                    pendingSession.MasterId);
            }
        }
    }

    private async Task ReconcileDeferredConnectionAbortsAsync(
        IReadOnlyList<IGameSession> deferredSessions,
        CancellationToken cancellationToken)
    {
        foreach (var session in deferredSessions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentSession = await _sessions.TryGetValue(session.ConnectionId);
            if (currentSession.Found && !ReferenceEquals(currentSession.Session, session))
            {
                _logger.LogCritical(
                    "Cannot reconcile deferred abort for connection '{connectionId}' because a different session is active; retaining the abort record until the connection ID is available.",
                    session.ConnectionId);
                continue;
            }

            try
            {
                _connectionTerminator.Abort(session);
                await _sessions.TryRemovePendingSessionAbort(session);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex,
                    "Failed to reconcile deferred abort for connection '{connectionId}'; it will be retried on the next lease cycle.",
                    session.ConnectionId);
            }
        }
    }

    private async Task AbortAndReconcileLostSession(IGameWorldSession session)
    {
        if (!await _sessions.TryMoveToPendingAbort(session))
        {
            _logger.LogCritical(
                "Could not reserve lost game session '{connectionId}' for abort reconciliation; the session was not removed.",
                session.ConnectionId);
            return;
        }

        var abortFailed = false;
        try
        {
            _connectionTerminator.Abort(session);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to abort lost game session '{connectionId}'. The abort will be retried.",
                session.ConnectionId);
            abortFailed = true;
        }
        finally
        {
            if (!abortFailed && !await _sessions.TryRemovePendingSessionAbort(session))
            {
                _logger.LogCritical(
                    "Could not clear the completed abort reservation for lost game session '{connectionId}'.",
                    session.ConnectionId);
            }
        }

        if (abortFailed && !_retryQueue.TryQueueConnectionAbort(
                session,
                () => _sessions.TryRemovePendingSessionAbort(session)))
        {
            _logger.LogWarning(
                "Could not queue a retry for lost game session '{connectionId}' because the cleanup retry capacity is full; retaining its atomic abort reservation for lease-worker reconciliation.",
                session.ConnectionId);
        }
    }
}
