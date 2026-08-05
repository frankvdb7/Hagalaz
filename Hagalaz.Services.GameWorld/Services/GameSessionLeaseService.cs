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
    private readonly IGameSessionAbortStore _abortSessions;
    private readonly IGameSessionClaimStore _claims;
    private readonly ILogger<GameSessionLeaseService> _logger;
    private readonly GameSessionAbortCoordinator _abortCoordinator;

    public GameSessionLeaseService(
        IGameSessionStore sessions,
        IGameSessionAbortStore abortSessions,
        IGameSessionClaimStore claims,
        ILogger<GameSessionLeaseService> logger,
        GameSessionAbortCoordinator abortCoordinator)
    {
        _sessions = sessions;
        _abortSessions = abortSessions;
        _claims = claims;
        _logger = logger;
        _abortCoordinator = abortCoordinator;
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
        var deferredAbortSessions = await _abortSessions.FindSessionsPendingAbort();
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
                await AbortAndReconcileLostSession(worldSession, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to renew active game-session claim for account '{masterId}'. Aborting the connection.",
                    worldSession.MasterId);
                await AbortAndReconcileLostSession(worldSession, cancellationToken);
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
            try
            {
                await _abortCoordinator.AbortPendingSessionAsync(session, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to reconcile deferred connection abort for '{connectionId}'; it will be retried on the next lease cycle.",
                    session.ConnectionId);
            }
        }
    }

    private async Task AbortAndReconcileLostSession(
        IGameWorldSession session,
        CancellationToken cancellationToken)
    {
        await _abortCoordinator.ReserveAndAbortLostSessionAsync(session, cancellationToken);
    }
}
