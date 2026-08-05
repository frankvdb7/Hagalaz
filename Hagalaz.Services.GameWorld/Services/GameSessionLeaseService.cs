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
        foreach (var pendingSession in pendingCleanupSessions)
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

    }

    private async Task AbortAndReconcileLostSession(IGameWorldSession session)
    {
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
            await ReconcileLostSession(session);
        }

        if (abortFailed && !_retryQueue.TryQueueConnectionAbort(session))
        {
            _logger.LogWarning(
                "Could not queue a retry for lost game session '{connectionId}' because the cleanup retry capacity is full.",
                session.ConnectionId);
        }
    }

    private async Task ReconcileLostSession(IGameWorldSession session)
    {
        var removedSession = await _sessions.TryRemove(session);
        if (removedSession.Removed)
        {
            _logger.LogInformation("Removed local game session for account '{masterId}' after losing session claim '{sessionClaimId}'.",
                session.MasterId, session.SessionClaimId);
        }
    }
}
