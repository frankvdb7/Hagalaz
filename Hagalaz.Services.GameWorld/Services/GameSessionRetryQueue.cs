using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Services;

/// <summary>
/// Owns bounded session-cleanup retry storage and processing independently of the application's global work queue.
/// </summary>
public sealed class GameSessionRetryQueue : BackgroundService
{
    public const int DefaultCapacity = 100;
    public const int DefaultOverflowCapacity = 100;

    private readonly Channel<IRetryWorkItem> _items;
    private readonly IGameSessionClaimStore _claims;
    private readonly IGameSessionConnectionTerminator _connectionTerminator;
    private readonly IGameSessionAbortStore _abortSessions;
    private readonly ILogger<GameSessionRetryQueue> _logger;
    private readonly SemaphoreSlim _retrySlots;
    private readonly TimeSpan _retryBackoff;
    private readonly object _overflowGate = new();
    private readonly Dictionary<RetryWorkKey, IRetryWorkItem> _overflowItems = new();
    private readonly int _overflowCapacity;
    private int _pendingCount;

    public GameSessionRetryQueue(
        IGameSessionClaimStore claims,
        IGameSessionConnectionTerminator connectionTerminator,
        ILogger<GameSessionRetryQueue> logger,
        IGameSessionAbortStore abortSessions)
        : this(
            claims,
            connectionTerminator,
            logger,
            abortSessions,
            DefaultCapacity,
            GameSessionClaimOptions.RenewalInterval,
            DefaultOverflowCapacity)
    {
    }

    public GameSessionRetryQueue(
        IGameSessionClaimStore claims,
        IGameSessionConnectionTerminator connectionTerminator,
        ILogger<GameSessionRetryQueue> logger,
        IGameSessionAbortStore abortSessions,
        int capacity,
        TimeSpan retryBackoff,
        int overflowCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(overflowCapacity);
        _claims = claims;
        _connectionTerminator = connectionTerminator;
        _abortSessions = abortSessions;
        _logger = logger;
        _retrySlots = new SemaphoreSlim(capacity, capacity);
        _retryBackoff = retryBackoff;
        _overflowCapacity = overflowCapacity;
        _items = Channel.CreateBounded<IRetryWorkItem>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    internal int PendingCount
    {
        get
        {
            lock (_overflowGate)
            {
                return Volatile.Read(ref _pendingCount) +
                    _overflowItems.Count;
            }
        }
    }

    public bool TryQueueClaimRelease(uint masterId, string claimId)
        => TryQueueWorkItem(new ClaimReleaseWorkItem(masterId, claimId));

    public bool TryQueueConnectionAbort(IGameSession session) =>
        TryQueueWorkItem(new ConnectionAbortWorkItem(session));

    public bool TryQueuePendingAbort(IGameSession session) =>
        TryQueueWorkItem(new PendingAbortWorkItem(session));

    private bool TryQueueWorkItem(IRetryWorkItem workItem)
    {
        if (_retrySlots.Wait(0))
        {
            if (_items.Writer.TryWrite(workItem))
            {
                Interlocked.Increment(ref _pendingCount);
                return true;
            }

            _retrySlots.Release();
        }

        // Lease reconciliation must not wait for the bounded primary queue. Keep one
        // retry per connection in a bounded overflow registry and let the worker
        // admit it as soon as a retry slot becomes available.
        return TryQueueOverflow(workItem);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            DrainOverflow();
            if (!await _items.Reader.WaitToReadAsync(stoppingToken))
            {
                return;
            }

            while (_items.Reader.TryRead(out var workItem))
            {
                Interlocked.Decrement(ref _pendingCount);
                await workItem.ExecuteAsync(this, stoppingToken);
                DrainOverflow();
            }
        }
    }

    internal async Task ProcessNextAsync(CancellationToken cancellationToken)
    {
        DrainOverflow();
        var workItem = await _items.Reader.ReadAsync(cancellationToken);
        Interlocked.Decrement(ref _pendingCount);
        await workItem.ExecuteAsync(this, cancellationToken);
        DrainOverflow();
    }

    private async ValueTask EnqueueAsync(
        IRetryWorkItem workItem,
        CancellationToken cancellationToken)
    {
        await _items.Writer.WriteAsync(workItem, cancellationToken);
        Interlocked.Increment(ref _pendingCount);
    }

    private void DrainOverflow()
    {
        while (_retrySlots.Wait(0))
        {
            var moved = false;
            lock (_overflowGate)
            {
                KeyValuePair<RetryWorkKey, IRetryWorkItem>? candidate = null;
                foreach (var entry in _overflowItems)
                {
                    if (entry.Value is ClaimReleaseWorkItem)
                    {
                        candidate = entry;
                        break;
                    }
                }

                if (!candidate.HasValue)
                {
                    foreach (var entry in _overflowItems)
                    {
                        candidate = entry;
                        break;
                    }
                }

                if (candidate.HasValue && _items.Writer.TryWrite(candidate.Value.Value))
                {
                    _overflowItems.Remove(candidate.Value.Key);
                    Interlocked.Increment(ref _pendingCount);
                    moved = true;
                }
            }

            if (!moved)
            {
                _retrySlots.Release();
                return;
            }
        }
    }

    private bool TryQueueOverflow(IRetryWorkItem retry)
    {
        var accepted = false;
        var coalesced = false;
        lock (_overflowGate)
        {
            if (_overflowItems.ContainsKey(retry.Key))
            {
                if (retry is PendingAbortWorkItem &&
                    _overflowItems[retry.Key] is ConnectionAbortWorkItem)
                {
                    _overflowItems[retry.Key] = retry;
                }

                coalesced = true;
            }
            else if (_overflowItems.Count < _overflowCapacity)
            {
                _overflowItems.Add(retry.Key, retry);
                accepted = true;
            }
        }

        if (!accepted && !coalesced)
        {
            LogRejectedOverflow(retry);
        }

        return accepted || coalesced;
    }

    private void LogRejectedOverflow(IRetryWorkItem retry)
    {
        switch (retry)
        {
            case ClaimReleaseWorkItem claimRelease:
                _logger.LogError(
                    "Rejected world-session claim-release retry for account '{masterId}' because the overflow cleanup capacity of {overflowCapacity} is full.",
                    claimRelease.MasterId,
                    _overflowCapacity);
                break;
            case IConnectionAbortWorkItem connectionAbort:
                _logger.LogError(
                    "Rejected connection-abort retry for session '{connectionId}' because the overflow cleanup capacity of {overflowCapacity} is full.",
                    connectionAbort.Session.ConnectionId,
                    _overflowCapacity);
                break;
        }
    }

    private async ValueTask ExecuteClaimReleaseAttemptAsync(uint masterId, string claimId, CancellationToken stoppingToken)
    {
        try
        {
            if (!await _claims.ReleaseAsync(masterId, claimId, stoppingToken))
            {
                _logger.LogInformation(
                    "World-session claim '{sessionClaimId}' for account '{masterId}' was already released or no longer has the exact owner.",
                    claimId,
                    masterId);
            }

            _retrySlots.Release();
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _retrySlots.Release();
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Retry failed while releasing world-session claim '{sessionClaimId}' for account '{masterId}'. The claim release will be retried.",
                claimId,
                masterId);
            _ = RequeueClaimReleaseAsync(masterId, claimId, stoppingToken);
        }
    }

    private async ValueTask ExecuteConnectionAbortAttemptAsync(
        IGameSession session,
        CancellationToken stoppingToken)
    {
        try
        {
            _connectionTerminator.Abort(session);
            _retrySlots.Release();
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _retrySlots.Release();
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Retry failed while aborting replaced game session '{connectionId}'. The abort will be retried.",
                session.ConnectionId);
            _ = RequeueAsync(new ConnectionAbortWorkItem(session), stoppingToken);
        }
    }

    private async ValueTask ExecutePendingAbortAttemptAsync(
        IGameSession session,
        CancellationToken stoppingToken)
    {
        try
        {
            if (!await _abortSessions.TryBeginPendingSessionAbort(session))
            {
                _retrySlots.Release();
                return;
            }

            _connectionTerminator.Abort(session);
            if (!await _abortSessions.TryCompletePendingSessionAbort(session))
            {
                _logger.LogCritical(
                    "Connection-abort retry succeeded for session '{connectionId}', but its pending-abort reservation could not be cleared.",
                    session.ConnectionId);
            }

            _retrySlots.Release();
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            await _abortSessions.TryReleasePendingSessionAbort(session);
            _retrySlots.Release();
            throw;
        }
        catch (Exception ex)
        {
            await _abortSessions.TryReleasePendingSessionAbort(session);
            _logger.LogWarning(ex,
                "Retry failed while aborting replaced game session '{connectionId}'. The abort will be retried.",
                session.ConnectionId);
            _ = RequeueAsync(new PendingAbortWorkItem(session), stoppingToken);
        }
    }

    private async Task RequeueClaimReleaseAsync(uint masterId, string claimId, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(_retryBackoff, stoppingToken);
            await EnqueueAsync(new ClaimReleaseWorkItem(masterId, claimId), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _retrySlots.Release();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Unable to requeue world-session claim release for account '{masterId}'. The cleanup retry is being dropped.",
                masterId);
            _retrySlots.Release();
        }
    }

    private async Task RequeueAsync(
        IRetryWorkItem workItem,
        CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(_retryBackoff, stoppingToken);
            await EnqueueAsync(workItem, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _retrySlots.Release();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Unable to requeue session cleanup work. The cleanup retry is being dropped.");
            _retrySlots.Release();
        }
    }

    private interface IRetryWorkItem
    {
        RetryWorkKey Key { get; }
        ValueTask ExecuteAsync(GameSessionRetryQueue queue, CancellationToken stoppingToken);
    }

    private interface IConnectionAbortWorkItem : IRetryWorkItem
    {
        IGameSession Session { get; }
    }

    private sealed record ClaimReleaseWorkItem(uint MasterId, string ClaimId) : IRetryWorkItem
    {
        public RetryWorkKey Key => new(RetryWorkKind.ClaimRelease, MasterId, ClaimId, null);

        public ValueTask ExecuteAsync(GameSessionRetryQueue queue, CancellationToken stoppingToken) =>
            queue.ExecuteClaimReleaseAttemptAsync(MasterId, ClaimId, stoppingToken);
    }

    private sealed record ConnectionAbortWorkItem(IGameSession Session) : IConnectionAbortWorkItem
    {
        public RetryWorkKey Key => new(RetryWorkKind.ConnectionAbort, 0, null, Session.ConnectionId);

        public ValueTask ExecuteAsync(GameSessionRetryQueue queue, CancellationToken stoppingToken) =>
            queue.ExecuteConnectionAbortAttemptAsync(Session, stoppingToken);
    }

    private sealed record PendingAbortWorkItem(IGameSession Session) : IConnectionAbortWorkItem
    {
        public RetryWorkKey Key => new(RetryWorkKind.ConnectionAbort, 0, null, Session.ConnectionId);

        public ValueTask ExecuteAsync(GameSessionRetryQueue queue, CancellationToken stoppingToken) =>
            queue.ExecutePendingAbortAttemptAsync(Session, stoppingToken);
    }

    private enum RetryWorkKind
    {
        ClaimRelease,
        ConnectionAbort
    }

    private readonly record struct RetryWorkKey(
        RetryWorkKind Kind,
        uint MasterId,
        string? ClaimId,
        string? ConnectionId);
}
