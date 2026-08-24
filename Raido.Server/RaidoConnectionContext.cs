using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Raido.Common.Messages;
using Raido.Common.Protocol;
using Raido.Server.Internal;

namespace Raido.Server;

/// <summary>
/// Represents the stable application connection for a Raido logical session.
/// </summary>
public class RaidoConnectionContext
{
    private static readonly WaitCallback _abortedCallback = AbortConnection;
    private readonly TaskCompletionSource _abortCompletedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly RaidoApplicationConnection _application;
    private readonly CancellationTokenSource _connectionAbortedTokenSource = new();
    private readonly IFeatureCollection _features;
    private readonly IDictionary<object, object?> _items;
    private readonly ILogger _logger;
    private readonly Lock _lifecycleLock = new();
    private readonly Lock _receiveMessageTimeoutLock = new();
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _writeLock = new(1);
    private readonly bool _statefulReconnectSupported;
    private readonly TimeSpan _statefulReconnectGracePeriod;
    private readonly TimeSpan _keepAliveInterval;
    private readonly TimeSpan _clientTimeoutInterval;
    private CancellationTokenRegistration _closedRequestedRegistration;
    private CancellationTokenRegistration _closedRegistration;
    private RaidoPhysicalConnectionSession? _physicalSession;
    private Task? _physicalTask;
    private TaskCompletionSource<bool> _rebindTcs = NewRebindTcs();
    private Task _previousPhysicalPumpsStopped = Task.CompletedTask;
    private readonly List<Func<PipeWriter, Task>> _reconnectedCallbacks = new();
    private RaidoRebindReservation? _rebindReservation;
    private ITimer? _graceTimer;
    private ClaimsPrincipal? _user;
    private bool _clientTimeoutActive;
    private bool _connectionAborted;
    private bool _statefulReconnectEnabled;
    private bool _statefulReconnectVetoed;
    private RaidoConnectionLifecycleState _lifecycleState = RaidoConnectionLifecycleState.Connected;
    private string _physicalConnectionId;
    private long _lastSendTick;
    private TimeSpan _receivedMessageElapsed;
    private bool _receivedMessageTimeoutEnabled;
    private long _receivedMessageTick;

    internal long StartTimestamp { get; set; }
    internal RaidoCallerContext RaidoCallerContext { get; }
    internal IRaidoCallerClients RaidoCallerClients { get; set; } = null!;
    internal Exception? CloseException { get; private set; }
    internal Activity? OriginalActivity { get; set; }
    internal MetricsContext MetricsContext { get; set; }
    internal RaidoApplicationConnection Application => _application;
    internal bool ApplicationWasTransferred => _application.Completion.IsCompletedSuccessfully &&
        _application.Completion.Result == RaidoApplicationExitReason.Transferred;

    public virtual CancellationToken ConnectionAbortedToken { get; }
    public virtual string ConnectionId { get; }
    public virtual string LogicalConnectionId => ConnectionId;

    public virtual string PhysicalConnectionId
    {
        get
        {
            lock (_lifecycleLock)
            {
                return _physicalSession?.ConnectionId ?? _physicalConnectionId;
            }
        }
    }

    public RaidoConnectionLifecycleState LifecycleState
    {
        get { lock (_lifecycleLock) return _lifecycleState; }
    }

    public virtual ClaimsPrincipal? User
    {
        get
        {
            if (_user is null) _user = Features.Get<IConnectionUserFeature>()?.User;
            return _user;
        }
    }

    public virtual IFeatureCollection Features => _features;
    public virtual IDictionary<object, object?> Items => _items;
    public virtual PipeReader Input => _application.Input;
    public virtual PipeWriter Output => _application.Output;
    public virtual IPEndPoint? LocalEndPoint { get { lock (_lifecycleLock) return _physicalSession?.LocalEndPoint; } }
    public virtual IPEndPoint? RemoteEndPoint { get { lock (_lifecycleLock) return _physicalSession?.RemoteEndPoint; } }
    public virtual IRaidoProtocol Protocol { get; internal set; } = default!;

    public RaidoConnectionContext(ConnectionContext connection, RaidoConnectionContextOptions contextOptions, ILoggerFactory loggerFactory)
        : this(new RaidoApplicationConnection(), new RaidoPhysicalConnectionSession(connection, loggerFactory), connection.Features,
            connection.Items, connection.ConnectionId, contextOptions, loggerFactory)
    {
    }

    internal RaidoConnectionContext(RaidoApplicationConnection application, RaidoPhysicalConnectionSession physicalSession,
        IFeatureCollection features, IDictionary<object, object?> items, RaidoConnectionContextOptions contextOptions, ILoggerFactory loggerFactory)
        : this(application, physicalSession, features, items, physicalSession.ConnectionId, contextOptions, loggerFactory)
    {
    }

    private RaidoConnectionContext(RaidoApplicationConnection application, RaidoPhysicalConnectionSession physicalSession,
        IFeatureCollection features, IDictionary<object, object?> items, string connectionId,
        RaidoConnectionContextOptions contextOptions, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(physicalSession);
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(contextOptions);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _application = application;
        _features = features;
        _items = items;
        ConnectionId = connectionId;
        _physicalConnectionId = physicalSession.ConnectionId;
        _logger = loggerFactory.CreateLogger<RaidoConnectionContext>();
        _clientTimeoutInterval = contextOptions.ClientTimeoutInterval;
        _keepAliveInterval = contextOptions.KeepAliveInterval;
        _statefulReconnectSupported = contextOptions.StatefulReconnectEnabled;
        _statefulReconnectGracePeriod = contextOptions.StatefulReconnectGracePeriod;
        if (_statefulReconnectSupported && (_statefulReconnectGracePeriod <= TimeSpan.Zero || _statefulReconnectGracePeriod == Timeout.InfiniteTimeSpan))
            throw new ArgumentOutOfRangeException(nameof(contextOptions), "The reconnect grace period must be positive and finite.");
        _timeProvider = contextOptions.TimeProvider ?? TimeProvider.System;
        ConnectionAbortedToken = _connectionAbortedTokenSource.Token;
        RaidoCallerContext = new DefaultRaidoCallerContext(this);
        AttachPhysicalUnsafe(physicalSession);
        if (_statefulReconnectSupported) _features.Set<IRaidoStatefulReconnectFeature>(new StatefulReconnectFeature(this));
        _lastSendTick = _timeProvider.GetTimestamp();
    }

    internal Task StartPhysicalSession()
    {
        lock (_lifecycleLock)
        {
            if (_physicalTask is not null)
            {
                return _physicalTask;
            }

            if (_physicalSession is null)
            {
                return Task.CompletedTask;
            }

            _physicalSession.Attach(this, _application);
            _physicalTask = _physicalSession.RunAsync(this, _application);
            return _physicalTask;
        }
    }


    internal Task OnConnectedAsync()
    {
        if (Features.Get<IConnectionInherentKeepAliveFeature>()?.HasInherentKeepAlive != true)
            Features.Get<IConnectionHeartbeatFeature>()?.OnHeartbeat(state => ((RaidoConnectionContext)state).KeepAliveTick(), this);
        StartTimestamp = _timeProvider.GetTimestamp();
        return Task.CompletedTask;
    }

    public virtual ValueTask WriteAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : RaidoMessage =>
        WriteAsync(message, false, null, cancellationToken);
    public virtual ValueTask WriteAsync<TMessage>(TMessage message, IRaidoProtocol protocol, CancellationToken cancellationToken = default)
        where TMessage : RaidoMessage
    {
        ArgumentNullException.ThrowIfNull(protocol);
        return WriteAsync(message, false, protocol, cancellationToken);
    }
    internal ValueTask WriteAsync<TMessage>(TMessage message, bool ignoreAbort, CancellationToken cancellationToken = default) where TMessage : RaidoMessage =>
        WriteAsync(message, ignoreAbort, null, cancellationToken);

    private ValueTask WriteAsync<TMessage>(TMessage message, bool ignoreAbort, IRaidoProtocol? protocolOverride, CancellationToken cancellationToken)
        where TMessage : RaidoMessage
    {
#pragma warning disable CA2016
        if (!_writeLock.Wait(0))
#pragma warning restore CA2016
            return new ValueTask(WriteSlowAsync(message, ignoreAbort, protocolOverride, cancellationToken));
        if (IsReconnecting() || (_connectionAborted && !ignoreAbort))
        {
            _writeLock.Release();
            return IsReconnecting() ? ValueTask.FromException(new RaidoConnectionReconnectingException(ConnectionId)) : default;
        }
        var task = WriteCore(message, protocolOverride ?? Protocol, cancellationToken);
        if (!task.IsCompletedSuccessfully) return new ValueTask(CompleteWriteAsync(task));
        task.GetAwaiter().GetResult();
        _writeLock.Release();
        return default;
    }

    private ValueTask<FlushResult> WriteCore<TMessage>(TMessage message, IRaidoProtocol protocol, CancellationToken cancellationToken)
        where TMessage : RaidoMessage
    {
        try
        {
            protocol.WriteMessage(message, _application.Output);
            if (!_application.Output.CanGetUnflushedBytes || _application.Output.UnflushedBytes > 0) Log.SentMessage(_logger, message);
            return _application.Output.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            CloseException = ex;
            Log.FailedWritingMessage(_logger, ex);
            AbortAllowReconnect(ex);
            return new ValueTask<FlushResult>(new FlushResult(false, true));
        }
    }

    private async Task CompleteWriteAsync(ValueTask<FlushResult> task)
    {
        try { await task.ConfigureAwait(false); }
        catch (Exception ex) { CloseException = ex; Log.FailedWritingMessage(_logger, ex); AbortAllowReconnect(ex); }
        finally { _writeLock.Release(); }
    }

    private async Task WriteSlowAsync<TMessage>(TMessage message, bool ignoreAbort, IRaidoProtocol? protocolOverride, CancellationToken cancellationToken)
        where TMessage : RaidoMessage
    {
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (IsReconnecting()) throw new RaidoConnectionReconnectingException(ConnectionId);
            if (!_connectionAborted || ignoreAbort) await WriteCore(message, protocolOverride ?? Protocol, cancellationToken).ConfigureAwait(false);
        }
        catch (RaidoConnectionReconnectingException) { throw; }
        catch (Exception ex) { CloseException = ex; Log.FailedWritingMessage(_logger, ex); AbortAllowReconnect(ex); }
        finally { _writeLock.Release(); }
    }

    public virtual void Abort()
    {
        RaidoPhysicalConnectionSession? physical;
        RaidoRebindReservation? reservation;
        bool queueAbort;
        lock (_lifecycleLock)
        {
            if (_lifecycleState == RaidoConnectionLifecycleState.Closed) return;
            _statefulReconnectEnabled = false;
            _statefulReconnectVetoed = true;
            _lifecycleState = RaidoConnectionLifecycleState.Closed;
            _connectionAborted = true;
            _graceTimer?.Dispose();
            _graceTimer = null;
            _rebindTcs.TrySetResult(false);
            reservation = _rebindReservation;
            _rebindReservation = null;
            physical = DetachPhysicalUnsafe();
            queueAbort = !_connectionAbortedTokenSource.IsCancellationRequested;
        }
        reservation?.Invalidate();
        physical?.Abort();
        _application.Complete(RaidoApplicationExitReason.Terminal);
        if (queueAbort) ThreadPool.QueueUserWorkItem(_abortedCallback, this);
    }

    internal void AbortAllowReconnect(Exception? exception = null)
    {
        RaidoPhysicalConnectionSession? physical;
        RaidoRebindReservation? reservation;
        bool queueAbort = false;
        lock (_lifecycleLock)
        {
            if (_lifecycleState is RaidoConnectionLifecycleState.Closed or RaidoConnectionLifecycleState.Reconnecting) return;
            CloseException ??= exception;
            _previousPhysicalPumpsStopped = _physicalSession?.PumpsStopped ?? Task.CompletedTask;
            reservation = _rebindReservation;
            _rebindReservation = null;
            physical = DetachPhysicalUnsafe();
            if (!_statefulReconnectEnabled || _statefulReconnectVetoed)
            {
                _lifecycleState = RaidoConnectionLifecycleState.Closed;
                _connectionAborted = true;
                _rebindTcs.TrySetResult(false);
                queueAbort = !_connectionAbortedTokenSource.IsCancellationRequested;
            }
            else StartReconnectUnsafe();
        }
        reservation?.Invalidate();
        physical?.Abort();
        if (queueAbort)
        {
            _application.Complete(RaidoApplicationExitReason.Terminal, exception);
            ThreadPool.QueueUserWorkItem(_abortedCallback, this);
        }
    }

    internal Task AbortAsync()
    {
        Abort();
        if (!_writeLock.Wait(0)) return AbortAsyncSlow();
        _writeLock.Release();
        return _abortCompletedTcs.Task;
    }
    private async Task AbortAsyncSlow()
    {
        await _writeLock.WaitAsync().ConfigureAwait(false);
        _writeLock.Release();
        await _abortCompletedTcs.Task.ConfigureAwait(false);
    }

    internal ValueTask<RaidoRebindReservation?> TryPrepareRebindAsync(RaidoConnectionContext replacement, IRaidoProtocol? replacementProtocol)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        if (ReferenceEquals(this, replacement)) return ValueTask.FromResult<RaidoRebindReservation?>(null);
        RaidoPhysicalConnectionSession? session;
        lock (replacement._lifecycleLock)
        {
            session = replacement._physicalSession;
            if (session is null || !session.CanAcceptTransfer(replacement)) return ValueTask.FromResult<RaidoRebindReservation?>(null);
        }
        var transfer = new RaidoApplicationTransfer(replacement, this, session, replacementProtocol);
        var reservation = new RaidoRebindReservation(transfer);
        transfer.Reservation = reservation;
        if (!session.TryReserveTransfer(transfer)) return ValueTask.FromResult<RaidoRebindReservation?>(null);

        lock (_lifecycleLock)
        {
            if (_lifecycleState != RaidoConnectionLifecycleState.Reconnecting || _physicalSession is not null || _rebindReservation is not null)
            {
                session.ClearReservedTransfer(transfer);
                return ValueTask.FromResult<RaidoRebindReservation?>(null);
            }

            _rebindReservation = reservation;
        }

        return ValueTask.FromResult<RaidoRebindReservation?>(reservation);
    }

    public Task<bool> WaitForRebindOrCloseAsync() => _rebindTcs.Task;
    public Task WaitForTerminationAsync() => _abortCompletedTcs.Task;

    private void EnableStatefulReconnect()
    {
        lock (_lifecycleLock)
        {
            if (_statefulReconnectSupported && !_statefulReconnectVetoed && _lifecycleState != RaidoConnectionLifecycleState.Closed)
                _statefulReconnectEnabled = true;
        }
    }
    private void DisableStatefulReconnect()
    {
        bool close;
        lock (_lifecycleLock)
        {
            _statefulReconnectVetoed = true;
            _statefulReconnectEnabled = false;
            close = _lifecycleState == RaidoConnectionLifecycleState.Reconnecting;
        }
        if (close) Abort();
    }
    private void StartReconnectUnsafe()
    {
        _lifecycleState = RaidoConnectionLifecycleState.Reconnecting;
        _rebindTcs = NewRebindTcs();
        _graceTimer = _timeProvider.CreateTimer(static state => ((RaidoConnectionContext)state!).GraceExpired(), this,
            _statefulReconnectGracePeriod, Timeout.InfiniteTimeSpan);
    }
    private static TaskCompletionSource<bool> NewRebindTcs() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class StatefulReconnectFeature(RaidoConnectionContext connection) : IRaidoStatefulReconnectFeature
    {
        public void EnableReconnect() => connection.EnableStatefulReconnect();
        public void DisableReconnect() => connection.DisableStatefulReconnect();
        public void OnReconnected(Func<PipeWriter, Task> callback) => connection.SetReconnectedCallback(callback);
    }
    private void SetReconnectedCallback(Func<PipeWriter, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        lock (_lifecycleLock) _reconnectedCallbacks.Add(callback);
    }

    internal bool CommitRebind(RaidoApplicationTransfer transfer)
    {
        lock (_lifecycleLock)
        {
            if (_lifecycleState != RaidoConnectionLifecycleState.Reconnecting || !ReferenceEquals(_rebindReservation?.Transfer, transfer)) return false;
            Protocol = transfer.Protocol ?? Protocol;
            AttachPhysicalUnsafe(transfer.Session);
            _rebindReservation = null;
            _graceTimer?.Dispose();
            _graceTimer = null;
            _lifecycleState = RaidoConnectionLifecycleState.Connected;
            _rebindTcs.TrySetResult(true);
            _rebindTcs = NewRebindTcs();
        }
        return true;
    }
    internal async Task InvokeReconnectedAsync()
    {
        Func<PipeWriter, Task>[] callbacks;
        lock (_lifecycleLock) callbacks = _reconnectedCallbacks.ToArray();
        foreach (var callback in callbacks)
        {
            try { await callback(Output).ConfigureAwait(false); }
            catch (Exception ex) { CloseException = ex; AbortAllowReconnect(ex); }
        }
    }
    internal void RollbackRebind()
    {
        RaidoRebindReservation? reservation;
        lock (_lifecycleLock)
        {
            reservation = _rebindReservation;
            _rebindReservation = null;
        }
        reservation?.Invalidate();
    }
    internal RaidoApplicationTransfer? TakePendingTransfer()
    {
        lock (_lifecycleLock)
        {
            return _physicalSession?.TakeReservedTransfer();
        }
    }
    internal Task AcquireTransferWriteLockAsync() => _writeLock.WaitAsync();
    internal void ReleaseTransferWriteLock() => _writeLock.Release();
    internal Task WaitForPreviousPhysicalPumpsAsync() => _previousPhysicalPumpsStopped;
    internal void CompleteTransferred()
    {
        lock (_lifecycleLock)
        {
            DetachPhysicalUnsafe();
            _closedRegistration.Dispose();
            _closedRequestedRegistration.Dispose();
        }

        _application.Complete(RaidoApplicationExitReason.Transferred);
    }

    internal void OnPhysicalSessionEnded(RaidoPhysicalConnectionSession session)
    {
        bool queueAbort = false;
        lock (_lifecycleLock)
        {
            if (!ReferenceEquals(_physicalSession, session)) return;
            _previousPhysicalPumpsStopped = session.PumpsStopped;
            DetachPhysicalUnsafe();
            if (_lifecycleState == RaidoConnectionLifecycleState.Closed) return;
            if (_statefulReconnectEnabled && !_statefulReconnectVetoed) StartReconnectUnsafe();
            else
            {
                _lifecycleState = RaidoConnectionLifecycleState.Closed;
                _connectionAborted = true;
                _rebindTcs.TrySetResult(false);
                queueAbort = !_connectionAbortedTokenSource.IsCancellationRequested;
            }
        }
        if (queueAbort)
        {
            _application.Complete(RaidoApplicationExitReason.Terminal);
            ThreadPool.QueueUserWorkItem(_abortedCallback, this);
        }
    }
    private void AttachPhysicalUnsafe(RaidoPhysicalConnectionSession session)
    {
        _closedRegistration.Dispose();
        _closedRequestedRegistration.Dispose();
        _physicalSession = session;
        _physicalConnectionId = session.ConnectionId;
        _closedRegistration = session.ConnectionClosed.Register(() => OnPhysicalSessionEnded(session));
        _closedRequestedRegistration = session.Features.Get<IConnectionLifetimeNotificationFeature>()?.ConnectionClosedRequested.Register(Abort) ?? default;
    }
    private RaidoPhysicalConnectionSession? DetachPhysicalUnsafe()
    {
        var physical = _physicalSession;
        _physicalSession = null;
        return physical;
    }
    private void GraceExpired()
    {
        RaidoRebindReservation? reservation;
        lock (_lifecycleLock)
        {
            if (_lifecycleState != RaidoConnectionLifecycleState.Reconnecting) return;
            _lifecycleState = RaidoConnectionLifecycleState.Closed;
            _connectionAborted = true;
            _graceTimer = null;
            _rebindTcs.TrySetResult(false);
            reservation = _rebindReservation;
            _rebindReservation = null;
        }
        reservation?.Invalidate();
        _application.Complete(RaidoApplicationExitReason.Terminal);
        ThreadPool.QueueUserWorkItem(_abortedCallback, this);
    }

    internal void StartClientTimeout()
    {
        if (_clientTimeoutActive) return;
        _clientTimeoutActive = true;
        Features.Get<IConnectionHeartbeatFeature>()?.OnHeartbeat(state => ((RaidoConnectionContext)state).CheckClientTimeout(), this);
    }
    internal void BeginClientTimeout()
    {
        lock (_receiveMessageTimeoutLock) { _receivedMessageTimeoutEnabled = true; _receivedMessageTick = _timeProvider.GetTimestamp(); }
    }
    internal void StopClientTimeout()
    {
        lock (_receiveMessageTimeoutLock) { _receivedMessageElapsed = TimeSpan.Zero; _receivedMessageTick = 0; _receivedMessageTimeoutEnabled = false; }
    }
    private void CheckClientTimeout()
    {
        if (Debugger.IsAttached || _connectionAborted || LifecycleState != RaidoConnectionLifecycleState.Connected) return;
        lock (_receiveMessageTimeoutLock)
        {
            if (_receivedMessageTimeoutEnabled && _timeProvider.GetElapsedTime(_receivedMessageTick) >= _clientTimeoutInterval)
            {
                CloseException ??= new OperationCanceledException($"Client hasn't sent a message/ping within the configured {nameof(RaidoConnectionContextOptions.ClientTimeoutInterval)}.");
                RaidoEventSource.Log.ConnectionTimedOut(ConnectionId);
                AbortAllowReconnect(CloseException);
            }
        }
    }
    private void KeepAliveTick()
    {
        var timestamp = _timeProvider.GetTimestamp();
        if (_timeProvider.GetElapsedTime(Volatile.Read(ref _lastSendTick), timestamp) > _keepAliveInterval)
        {
            _ = TryWritePingAsync().AsTask();
            Volatile.Write(ref _lastSendTick, timestamp);
        }
    }
    private ValueTask TryWritePingAsync() => !_writeLock.Wait(0) ? default : new ValueTask(TryWritePingSlowAsync());
    private async Task TryWritePingSlowAsync()
    {
        try
        {
            if (!_connectionAborted && !IsReconnecting()) await _application.Output.WriteAsync(Protocol.GetMessageBytes(PingMessage.Instance)).ConfigureAwait(false);
        }
        catch (Exception ex) { CloseException = ex; AbortAllowReconnect(ex); }
        finally { _writeLock.Release(); }
    }
    internal void Cleanup()
    {
        _closedRequestedRegistration.Dispose();
        _closedRegistration.Dispose();
        _graceTimer?.Dispose();
    }
    private bool IsReconnecting() { lock (_lifecycleLock) return _lifecycleState == RaidoConnectionLifecycleState.Reconnecting; }
    private static void AbortConnection(object? state)
    {
        var connection = (RaidoConnectionContext)state!;
        try { connection._connectionAbortedTokenSource.Cancel(); }
        catch (Exception ex) { Log.AbortFailed(connection._logger, ex); }
        finally { _ = CompleteAbortAsync(connection); }
    }
    private static async Task CompleteAbortAsync(RaidoConnectionContext connection)
    {
        await connection._writeLock.WaitAsync().ConfigureAwait(false);
        connection._writeLock.Release();
        connection._abortCompletedTcs.TrySetResult();
    }

    private static class Log
    {
        private static readonly Action<ILogger, RaidoMessage, Exception?> _sentMessage = LoggerMessage.Define<RaidoMessage>(LogLevel.Trace, new EventId(2, "SentMessage"), "Sent a {Message} to the client.");
        private static readonly Action<ILogger, Exception> _failedWritingMessage = LoggerMessage.Define(LogLevel.Debug, new EventId(3, "FailedWritingMessage"), "Failed writing message. Aborting connection.");
        private static readonly Action<ILogger, Exception> _abortFailed = LoggerMessage.Define(LogLevel.Trace, new EventId(4, "AbortFailed"), "Abort callback failed.");
        public static void SentMessage(ILogger logger, RaidoMessage message) => _sentMessage(logger, message, null);
        public static void FailedWritingMessage(ILogger logger, Exception exception) => _failedWritingMessage(logger, exception);
        public static void AbortFailed(ILogger logger, Exception exception) => _abortFailed(logger, exception);
    }
}

public sealed class RaidoRebindReservation
{
    private readonly object _lock = new();
    private readonly RaidoApplicationTransfer _transfer;
    private readonly List<Func<Task>> _committedCallbacks = new();
    private bool _invalidated;

    internal RaidoRebindReservation(RaidoApplicationTransfer transfer) => _transfer = transfer;

    /// <summary>
    /// Registers work that is sent only after the replacement has committed.
    /// </summary>
    public void OnCommitted(Func<Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        lock (_lock)
        {
            if (_invalidated)
            {
                throw new InvalidOperationException("The reconnect reservation is no longer valid.");
            }

            _committedCallbacks.Add(callback);
        }
    }

    internal RaidoApplicationTransfer Transfer => _transfer;
    internal RaidoConnectionContext Source => _transfer.Source;
    internal RaidoConnectionContext Target => _transfer.Target;
    internal RaidoPhysicalConnectionSession Session => _transfer.Session;
    internal IRaidoProtocol? Protocol => _transfer.Protocol;

    internal void Invalidate()
    {
        lock (_lock)
        {
            _invalidated = true;
        }

        _transfer.Session.ClearReservedTransfer(_transfer);
    }

    internal async Task InvokeCommittedAsync()
    {
        Func<Task>[] callbacks;
        lock (_lock)
        {
            if (_invalidated)
            {
                return;
            }

            callbacks = _committedCallbacks.ToArray();
            _invalidated = true;
        }

        foreach (var callback in callbacks)
        {
            await callback().ConfigureAwait(false);
        }
    }
}

internal sealed class RaidoApplicationTransfer
{
    public RaidoApplicationTransfer(RaidoConnectionContext source, RaidoConnectionContext target, RaidoPhysicalConnectionSession session, IRaidoProtocol? protocol)
    {
        Source = source;
        Target = target;
        Session = session;
        Protocol = protocol;
    }
    public RaidoConnectionContext Source { get; }
    public RaidoConnectionContext Target { get; }
    public RaidoPhysicalConnectionSession Session { get; }
    public IRaidoProtocol? Protocol { get; }
    internal RaidoRebindReservation? Reservation { get; set; }
    public Task<bool> CommitAsync(Func<ValueTask<ReadOnlyMemory<byte>>> capturePendingInput) =>
        Session.CommitTransferAsync(this, Source, Target, Target.Application, capturePendingInput, Protocol);
}
