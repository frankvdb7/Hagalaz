using System;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Services;

/// <summary>
/// Coordinates the atomic local reservation and the external connection abort.
/// A failed abort releases only the processing marker; the reservation remains
/// available for the next lease reconciliation cycle.
/// </summary>
public sealed class GameSessionAbortCoordinator
{
    private readonly IGameSessionStore _sessions;
    private readonly IGameSessionAbortState _abortSessions;
    private readonly IGameSessionConnectionTerminator _connectionTerminator;
    private readonly ILogger<GameSessionAbortCoordinator> _logger;

    public GameSessionAbortCoordinator(
        IGameSessionStore sessions,
        IGameSessionAbortState abortSessions,
        IGameSessionConnectionTerminator connectionTerminator,
        ILogger<GameSessionAbortCoordinator> logger)
    {
        _sessions = sessions;
        _abortSessions = abortSessions;
        _connectionTerminator = connectionTerminator;
        _logger = logger;
    }

    public async Task<bool> ReserveAndAbortLostSessionAsync(
        IGameSession session,
        CancellationToken cancellationToken)
    {
        if (!await _abortSessions.TryMoveToPendingAbort(session))
        {
            _logger.LogCritical(
                "Could not reserve lost game session '{connectionId}' for abort reconciliation; the session was not removed.",
                session.ConnectionId);
            return false;
        }

        return await AbortPendingSessionAsync(session, cancellationToken);
    }

    public async Task<bool> AbortPendingSessionAsync(
        IGameSession session,
        CancellationToken cancellationToken)
    {
        var processingLease = await _abortSessions.TryBeginPendingSessionAbort(session);
        if (processingLease is not { } lease)
        {
            return false;
        }

        var currentSession = await _sessions.TryGetValue(session.ConnectionId);
        if (currentSession.Found && !ReferenceEquals(currentSession.Session, session))
        {
            await ReleaseProcessingMarkerAsync(session, lease);
            _logger.LogCritical(
                "Cannot reconcile deferred abort for connection '{connectionId}' because a different session is active; retaining the abort record until the connection ID is available.",
                session.ConnectionId);
            return false;
        }

        try
        {
            _connectionTerminator.Abort(session);
            if (!await _abortSessions.TryCompletePendingSessionAbort(session, lease))
            {
                _logger.LogCritical(
                    "Could not clear the completed abort reservation for session '{connectionId}'.",
                    session.ConnectionId);
                await ReleaseProcessingMarkerAsync(session, lease);
                return false;
            }

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await ReleaseProcessingMarkerAsync(session, lease);
            throw;
        }
        catch (OperationCanceledException ex)
        {
            await ReleaseProcessingMarkerAsync(session, lease);
            _logger.LogWarning(ex,
                "Abort of game session '{connectionId}' was canceled by the connection terminator; it will be retried on the next lease cycle.",
                session.ConnectionId);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await ReleaseProcessingMarkerAsync(session, lease);
            _logger.LogWarning(ex,
                "Failed to abort game session '{connectionId}'; it will be retried on the next lease cycle.",
                session.ConnectionId);
            return false;
        }
    }

    private async Task ReleaseProcessingMarkerAsync(IGameSession session, AbortProcessingLease processingLease)
    {
        try
        {
            if (!await _abortSessions.TryReleasePendingSessionAbort(session, processingLease))
            {
                _logger.LogCritical(
                    "Could not release the processing marker for pending abort '{connectionId}'.",
                    session.ConnectionId);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogCritical(ex,
                "Failed to release the processing marker for pending abort '{connectionId}'.",
                session.ConnectionId);
        }
    }
}
