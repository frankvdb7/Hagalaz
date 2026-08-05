using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
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

    private readonly Channel<Func<CancellationToken, ValueTask>> _items;
    private readonly IGameSessionClaimStore _claims;
    private readonly IGameSessionConnectionTerminator _connectionTerminator;
    private readonly ILogger<GameSessionRetryQueue> _logger;
    private readonly SemaphoreSlim _retrySlots;
    private readonly TimeSpan _retryBackoff;
    private readonly object _overflowGate = new();
    private readonly Dictionary<ClaimReleaseRetry, ClaimReleaseRetry> _overflowClaimReleases = new();
    private readonly Dictionary<string, IGameSession> _overflowConnectionAborts = new();
    private readonly int _overflowCapacity;
    private int _pendingCount;

    public GameSessionRetryQueue(
        IGameSessionClaimStore claims,
        IGameSessionConnectionTerminator connectionTerminator,
        ILogger<GameSessionRetryQueue> logger,
        int capacity = DefaultCapacity,
        TimeSpan? retryBackoff = null,
        int overflowCapacity = DefaultOverflowCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(overflowCapacity);
        _claims = claims;
        _connectionTerminator = connectionTerminator;
        _logger = logger;
        _retrySlots = new SemaphoreSlim(capacity, capacity);
        _retryBackoff = retryBackoff ?? GameSessionClaimOptions.RenewalInterval;
        _overflowCapacity = overflowCapacity;
        _items = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(new BoundedChannelOptions(capacity)
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
                    _overflowClaimReleases.Count +
                    _overflowConnectionAborts.Count;
            }
        }
    }

    public bool TryQueueClaimRelease(uint masterId, string claimId)
    {
        if (_retrySlots.Wait(0))
        {
            if (_items.Writer.TryWrite(stoppingToken => ExecuteClaimReleaseAttemptAsync(masterId, claimId, stoppingToken)))
            {
                Interlocked.Increment(ref _pendingCount);
                return true;
            }

            _retrySlots.Release();
        }

        return TryQueueOverflowClaimRelease(masterId, claimId);
    }

    public bool TryQueueConnectionAbort(IGameSession session)
    {
        if (_retrySlots.Wait(0))
        {
            if (_items.Writer.TryWrite(stoppingToken => ExecuteConnectionAbortAttemptAsync(session, stoppingToken)))
            {
                Interlocked.Increment(ref _pendingCount);
                return true;
            }

            _retrySlots.Release();
        }

        // Lease reconciliation must not wait for the bounded primary queue. Keep one
        // retry per connection in a bounded overflow registry and let the worker
        // admit it as soon as a retry slot becomes available.
        return TryQueueOverflowConnectionAbort(session);
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
                await workItem(stoppingToken);
                DrainOverflow();
            }
        }
    }

    internal async Task ProcessNextAsync(CancellationToken cancellationToken)
    {
        DrainOverflow();
        var workItem = await _items.Reader.ReadAsync(cancellationToken);
        Interlocked.Decrement(ref _pendingCount);
        await workItem(cancellationToken);
        DrainOverflow();
    }

    private async ValueTask EnqueueAsync(
        Func<CancellationToken, ValueTask> workItem,
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
                ClaimReleaseRetry? claimCandidate = null;
                foreach (var entry in _overflowClaimReleases)
                {
                    claimCandidate = entry.Value;
                    break;
                }

                if (claimCandidate.HasValue &&
                    _items.Writer.TryWrite(stoppingToken => ExecuteClaimReleaseAttemptAsync(
                        claimCandidate.Value.MasterId,
                        claimCandidate.Value.ClaimId,
                        stoppingToken)))
                {
                    _overflowClaimReleases.Remove(claimCandidate.Value);
                    Interlocked.Increment(ref _pendingCount);
                    moved = true;
                }
                else if (!claimCandidate.HasValue)
                {
                    KeyValuePair<string, IGameSession>? connectionCandidate = null;
                    foreach (var entry in _overflowConnectionAborts)
                    {
                        connectionCandidate = entry;
                        break;
                    }

                    if (connectionCandidate.HasValue &&
                        _items.Writer.TryWrite(stoppingToken => ExecuteConnectionAbortAttemptAsync(connectionCandidate.Value.Value, stoppingToken)))
                    {
                        _overflowConnectionAborts.Remove(connectionCandidate.Value.Key);
                        Interlocked.Increment(ref _pendingCount);
                        moved = true;
                    }
                }
            }

            if (!moved)
            {
                _retrySlots.Release();
                return;
            }
        }
    }

    private bool TryQueueOverflowClaimRelease(uint masterId, string claimId)
    {
        var retry = new ClaimReleaseRetry(masterId, claimId);
        var accepted = false;
        var coalesced = false;
        lock (_overflowGate)
        {
            if (_overflowClaimReleases.ContainsKey(retry))
            {
                coalesced = true;
            }
            else if (_overflowClaimReleases.Count + _overflowConnectionAborts.Count < _overflowCapacity)
            {
                _overflowClaimReleases.Add(retry, retry);
                accepted = true;
            }
        }

        if (!accepted && !coalesced)
        {
            _logger.LogError(
                "Rejected world-session claim-release retry for account '{masterId}' because the overflow cleanup capacity of {overflowCapacity} is full.",
                masterId,
                _overflowCapacity);
        }

        return accepted || coalesced;
    }

    private bool TryQueueOverflowConnectionAbort(IGameSession session)
    {
        var accepted = false;
        var coalesced = false;
        lock (_overflowGate)
        {
            if (_overflowConnectionAborts.ContainsKey(session.ConnectionId))
            {
                coalesced = true;
            }
            else if (_overflowClaimReleases.Count + _overflowConnectionAborts.Count < _overflowCapacity)
            {
                _overflowConnectionAborts.Add(session.ConnectionId, session);
                accepted = true;
            }
        }

        if (!accepted && !coalesced)
        {
            _logger.LogError(
                "Rejected connection-abort retry for session '{connectionId}' because the overflow cleanup capacity of {overflowCapacity} is full.",
                session.ConnectionId,
                _overflowCapacity);
        }

        return accepted || coalesced;
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

    private async ValueTask ExecuteConnectionAbortAttemptAsync(IGameSession session, CancellationToken stoppingToken)
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
            _ = RequeueConnectionAbortAsync(session, stoppingToken);
        }
    }

    private async Task RequeueClaimReleaseAsync(uint masterId, string claimId, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(_retryBackoff, stoppingToken);
            await EnqueueAsync(
                token => ExecuteClaimReleaseAttemptAsync(masterId, claimId, token),
                stoppingToken);
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

    private async Task RequeueConnectionAbortAsync(IGameSession session, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(_retryBackoff, stoppingToken);
            await EnqueueAsync(
                token => ExecuteConnectionAbortAttemptAsync(session, token),
                stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _retrySlots.Release();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Unable to requeue abort for replaced game session '{connectionId}'. The cleanup retry is being dropped.",
                session.ConnectionId);
            _retrySlots.Release();
        }
    }

    private readonly record struct ClaimReleaseRetry(uint MasterId, string ClaimId);
}
