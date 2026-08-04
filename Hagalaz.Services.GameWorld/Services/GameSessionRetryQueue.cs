using System;
using System.Collections.Concurrent;
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

    private readonly Channel<Func<CancellationToken, ValueTask>> _items;
    private readonly IGameSessionClaimStore _claims;
    private readonly IGameSessionConnectionTerminator _connectionTerminator;
    private readonly ILogger<GameSessionRetryQueue> _logger;
    private readonly SemaphoreSlim _retrySlots;
    private readonly TimeSpan _retryBackoff;
    private readonly ConcurrentDictionary<string, IGameSession> _overflowConnectionAborts = new();
    private int _pendingCount;

    public GameSessionRetryQueue(
        IGameSessionClaimStore claims,
        IGameSessionConnectionTerminator connectionTerminator,
        ILogger<GameSessionRetryQueue> logger,
        int capacity = DefaultCapacity,
        TimeSpan? retryBackoff = null)
    {
        _claims = claims;
        _connectionTerminator = connectionTerminator;
        _logger = logger;
        _retrySlots = new SemaphoreSlim(capacity, capacity);
        _retryBackoff = retryBackoff ?? GameSessionClaimOptions.RenewalInterval;
        _items = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    internal int PendingCount => Volatile.Read(ref _pendingCount) + _overflowConnectionAborts.Count;

    public async ValueTask QueueClaimReleaseAsync(uint masterId, string claimId, CancellationToken cancellationToken = default)
    {
        await _retrySlots.WaitAsync(cancellationToken);
        try
        {
            await EnqueueAsync(
                stoppingToken => ExecuteClaimReleaseAttemptAsync(masterId, claimId, stoppingToken),
                cancellationToken);
        }
        catch
        {
            _retrySlots.Release();
            throw;
        }
    }

    public async ValueTask QueueConnectionAbortAsync(IGameSession session, CancellationToken cancellationToken = default)
    {
        await _retrySlots.WaitAsync(cancellationToken);
        try
        {
            await EnqueueAsync(
                stoppingToken => ExecuteConnectionAbortAttemptAsync(session, stoppingToken),
                cancellationToken);
        }
        catch
        {
            _retrySlots.Release();
            throw;
        }
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
        // retry per connection in a coalesced overflow registry and let the worker
        // admit it as soon as a retry slot becomes available.
        _overflowConnectionAborts[session.ConnectionId] = session;
        return true;
    }

    public async ValueTask<ConnectionAbortReservation> ReserveConnectionAbortAsync(
        CancellationToken cancellationToken = default)
    {
        await _retrySlots.WaitAsync(cancellationToken);
        return new ConnectionAbortReservation(this);
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

    private bool TryEnqueueReservedConnectionAbort(IGameSession session)
    {
        if (_items.Writer.TryWrite(stoppingToken => ExecuteConnectionAbortAttemptAsync(session, stoppingToken)))
        {
            Interlocked.Increment(ref _pendingCount);
            return true;
        }

        _retrySlots.Release();
        return false;
    }

    private void DrainOverflow()
    {
        while (_retrySlots.Wait(0))
        {
            KeyValuePair<string, IGameSession>? overflow = null;
            foreach (var candidate in _overflowConnectionAborts)
            {
                if (_overflowConnectionAborts.TryRemove(candidate.Key, out var session))
                {
                    overflow = new KeyValuePair<string, IGameSession>(candidate.Key, session);
                    break;
                }
            }

            if (!overflow.HasValue)
            {
                _retrySlots.Release();
                return;
            }

            if (_items.Writer.TryWrite(stoppingToken => ExecuteConnectionAbortAttemptAsync(overflow.Value.Value, stoppingToken)))
            {
                Interlocked.Increment(ref _pendingCount);
                continue;
            }

            _overflowConnectionAborts.TryAdd(overflow.Value.Key, overflow.Value.Value);
            _retrySlots.Release();
            return;
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

    public sealed class ConnectionAbortReservation : IDisposable
    {
        private GameSessionRetryQueue? _queue;

        internal ConnectionAbortReservation(GameSessionRetryQueue queue) => _queue = queue;

        public bool TryQueueConnectionAbort(IGameSession session)
        {
            var queue = Interlocked.Exchange(ref _queue, null)
                ?? throw new ObjectDisposedException(nameof(ConnectionAbortReservation));
            return queue.TryEnqueueReservedConnectionAbort(session);
        }

        public void Dispose()
        {
            var queue = Interlocked.Exchange(ref _queue, null);
            if (queue == null)
            {
                return;
            }

            queue._retrySlots.Release();
            queue.DrainOverflow();
        }
    }
}
