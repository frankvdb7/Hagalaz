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

namespace Raido.Server
{
    /// <summary>
    /// Represents a connection to a Raido endpoint.
    /// </summary>
    public class RaidoConnectionContext
    {
        private static readonly WaitCallback _abortedCallback = AbortConnection;

        private readonly TaskCompletionSource _abortCompletedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private RaidoPhysicalTransport? _physicalTransport;
        private readonly CancellationTokenSource _connectionAbortedTokenSource = new();
        private CancellationTokenRegistration _closedRegistration;
        private CancellationTokenRegistration? _closedRequestedRegistration;
        private CancellationTokenSource _physicalAbortedTokenSource = new();
        private readonly IFeatureCollection _features;
        private readonly IDictionary<object, object?> _items;

        private readonly ILogger _logger;
        private readonly Lock _lifecycleLock = new();
        private readonly Lock _receiveMessageTimeoutLock = new();
        private readonly TimeProvider _timeProvider;
        private readonly SemaphoreSlim _dispatchLock = new(1);
        private readonly SemaphoreSlim _writeLock = new(1);
        private readonly bool _statefulReconnectSupported;
        private bool _statefulReconnectEnabled;
        private readonly TimeSpan _statefulReconnectGracePeriod;
        private ITimer? _graceTimer;
        private TaskCompletionSource<bool> _rebindTcs = NewRebindTcs();
        private ClaimsPrincipal? _user;

        private bool _clientTimeoutActive;
        private volatile bool _connectionAborted;
        private RaidoConnectionLifecycleState _lifecycleState = RaidoConnectionLifecycleState.Connected;
        private long _physicalGeneration;
        private string _physicalConnectionId;
        private long _lastSendTick;
        private TimeSpan _receivedMessageElapsed;
        private bool _receivedMessageTimeoutEnabled;
        private long _receivedMessageTick;
        private Func<PipeWriter, Task>? _reconnectedCallback;
        private Func<PipeWriter, Task>? _transportHandoffCallback;
        private bool _transportWasHandedOff;

        private readonly TimeSpan _keepAliveInterval;
        private readonly TimeSpan _clientTimeoutInterval;

        internal long StartTimestamp { get; set; }

        internal RaidoCallerContext RaidoCallerContext { get; }
        internal IRaidoCallerClients RaidoCallerClients { get; set; } = null!;

        internal Exception? CloseException { get; private set; }

        internal Activity? OriginalActivity { get; set; }

        internal MetricsContext MetricsContext { get; set; }

        /// <summary>
        /// Gets a <see cref="CancellationToken"/> that notifies when the connection is aborted.
        /// </summary>
        public virtual CancellationToken ConnectionAbortedToken { get; }

        /// <summary>
        /// Gets the ID for this connection.
        /// </summary>
        public virtual string ConnectionId { get; }

        /// <summary>
        /// Gets the stable logical connection id retained across physical transport replacement.
        /// </summary>
        public virtual string LogicalConnectionId => ConnectionId;

        /// <summary>
        /// Gets the current physical Kestrel connection id, or the last physical id while detached.
        /// </summary>
        public virtual string PhysicalConnectionId
        {
            get
            {
                lock (_lifecycleLock)
                {
                    return _physicalTransport?.ConnectionId ?? _physicalConnectionId;
                }
            }
        }

        /// <summary>
        /// Gets the current logical lifecycle state.
        /// </summary>
        public RaidoConnectionLifecycleState LifecycleState
        {
            get
            {
                lock (_lifecycleLock)
                {
                    return _lifecycleState;
                }
            }
        }

        /// <summary>
        /// Gets the user for this connection.
        /// </summary>
        public virtual ClaimsPrincipal? User
        {
            get
            {
                if (_user is null)
                {
                    _user = Features.Get<IConnectionUserFeature>()?.User;
                }

                return _user;
            }
        }

        /// <summary>
        /// Gets the collection of features available on this connection.
        /// </summary>
        public virtual IFeatureCollection Features => _features;

        /// <summary>
        /// Gets a key/value collection that can be used to share data within the scope of this connection.
        /// </summary>
        public virtual IDictionary<object, object?> Items => _items;

        /// <summary>
        /// Gets the input pipe for the connection.
        /// </summary>
        public virtual PipeReader Input => GetPhysicalConnection().Transport.Input;

        /// <summary>
        /// Gets the output pipe for the connection.
        /// </summary>
        public virtual PipeWriter Output => GetPhysicalConnection().Transport.Output;

        /// <summary>
        /// Gets the local endpoint for the connection.
        /// </summary>
        public virtual IPEndPoint? LocalEndPoint
        {
            get
            {
                lock (_lifecycleLock)
                {
                    return _physicalTransport?.LocalEndPoint;
                }
            }
        }

        /// <summary>
        /// Gets the remote endpoint for the connection.
        /// </summary>
        public virtual IPEndPoint? RemoteEndPoint
        {
            get
            {
                lock (_lifecycleLock)
                {
                    return _physicalTransport?.RemoteEndPoint;
                }
            }
        }


        /// <summary>
        /// Gets the protocol used by this connection.
        /// </summary>
        public virtual IRaidoProtocol Protocol { get; internal set; } = default!;

        /// <summary>
        /// Initializes a new instance of the <see cref="RaidoConnectionContext"/> class.
        /// </summary>
        /// <param name="connection">The underlying <see cref="ConnectionContext"/>.</param>
        /// <param name="contextOptions">The options for the connection context.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public RaidoConnectionContext(ConnectionContext connection, RaidoConnectionContextOptions contextOptions, ILoggerFactory loggerFactory)
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentNullException.ThrowIfNull(contextOptions);
            ArgumentNullException.ThrowIfNull(loggerFactory);

            _features = connection.Features;
            _items = connection.Items;
            ConnectionId = connection.ConnectionId;
            _physicalConnectionId = connection.ConnectionId;
            _logger = loggerFactory.CreateLogger<RaidoConnectionContext>();

            _clientTimeoutInterval = contextOptions.ClientTimeoutInterval;
            _keepAliveInterval = contextOptions.KeepAliveInterval;
            _statefulReconnectSupported = contextOptions.StatefulReconnectEnabled;
            _statefulReconnectEnabled = false;
            _statefulReconnectGracePeriod = contextOptions.StatefulReconnectGracePeriod;
            if (_statefulReconnectSupported && (_statefulReconnectGracePeriod <= TimeSpan.Zero || _statefulReconnectGracePeriod == Timeout.InfiniteTimeSpan))
            {
                throw new ArgumentOutOfRangeException(nameof(contextOptions), "The reconnect grace period must be positive and finite.");
            }

            _timeProvider = contextOptions.TimeProvider ?? TimeProvider.System;
            ConnectionAbortedToken = _connectionAbortedTokenSource.Token;
            AttachPhysicalUnsafe(new RaidoPhysicalTransport(connection));
            _features.Set<IRaidoTransportHandoffFeature>(new TransportHandoffFeature(this));
            if (_statefulReconnectSupported)
            {
                _features.Set<IRaidoStatefulReconnectFeature>(new StatefulReconnectFeature(this));
            }

            RaidoCallerContext = new DefaultRaidoCallerContext(this);

            _lastSendTick = _timeProvider.GetTimestamp();
        }

        internal CancellationToken PhysicalConnectionAbortedToken => _physicalAbortedTokenSource.Token;

        internal long PhysicalGeneration
        {
            get
            {
                lock (_lifecycleLock)
                {
                    return _physicalGeneration;
                }
            }
        }

        internal bool IsCurrentPhysicalGeneration(long generation)
        {
            lock (_lifecycleLock)
            {
                return _lifecycleState == RaidoConnectionLifecycleState.Connected &&
                    _physicalTransport is not null &&
                    _physicalGeneration == generation;
            }
        }

        internal async ValueTask<bool> EnterDispatchAsync(long generation)
        {
            await _dispatchLock.WaitAsync().ConfigureAwait(false);
            if (IsCurrentPhysicalGeneration(generation))
            {
                return true;
            }

            _dispatchLock.Release();
            return false;
        }

        internal void ExitDispatch() => _dispatchLock.Release();

        internal Task OnConnectedAsync()
        {
            if (Features.Get<IConnectionInherentKeepAliveFeature>()?.HasInherentKeepAlive != true)
            {
                Features.Get<IConnectionHeartbeatFeature>()?.OnHeartbeat(state => ((RaidoConnectionContext)state).KeepAliveTick(), this);
            }

            StartTimestamp = _timeProvider.GetTimestamp();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Writes a message to the connection.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="message">The message to write.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the write operation.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous write operation.</returns>
        public virtual ValueTask WriteAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : RaidoMessage =>
            WriteAsync<TMessage>(message, ignoreAbort: false, protocolOverride: null, cancellationToken: cancellationToken);

        /// <summary>
        /// Writes a message using a specific protocol during a protocol transition.
        /// </summary>
        public virtual ValueTask WriteAsync<TMessage>(
            TMessage message,
            IRaidoProtocol protocol,
            CancellationToken cancellationToken = default) where TMessage : RaidoMessage
        {
            ArgumentNullException.ThrowIfNull(protocol);
            return WriteAsync(message, ignoreAbort: false, protocolOverride: protocol, cancellationToken: cancellationToken);
        }

        internal ValueTask WriteAsync<TMessage>(
            TMessage message,
            bool ignoreAbort,
            CancellationToken cancellationToken = default) where TMessage : RaidoMessage =>
            WriteAsync(message, ignoreAbort, protocolOverride: null, cancellationToken: cancellationToken);

        private ValueTask WriteAsync<TMessage>(
            TMessage message,
            bool ignoreAbort,
            IRaidoProtocol? protocolOverride,
            CancellationToken cancellationToken)
            where TMessage : RaidoMessage
        {
            // Try to grab the lock synchronously, if we fail, go to the slower path
#pragma warning disable CA2016 // This will always finish synchronously so we do not need to both with cancel
            if (!_writeLock.Wait(0))
#pragma warning restore CA2016
            {
                return new ValueTask(WriteSlowAsync(message, ignoreAbort, protocolOverride, cancellationToken));
            }

            if (IsReconnecting())
            {
                Log.WriteWhileReconnecting(_logger, ConnectionId);
                _writeLock.Release();
                return ValueTask.FromException(new RaidoConnectionReconnectingException(ConnectionId));
            }

            if (_connectionAborted && !ignoreAbort)
            {
                _writeLock.Release();
                return default;
            }

            var connection = GetPhysicalTransport();

            // This method should never throw synchronously
            var task = WriteCore(message, connection.Transport.Output, protocolOverride ?? Protocol, cancellationToken);

            // The write didn't complete synchronously so await completion
            if (!task.IsCompletedSuccessfully)
            {
                return new ValueTask(CompleteWriteAsync(task));
            }
            else
            {
                // If it's a IValueTaskSource backed ValueTask,
                // inform it its result has been read so it can reset
                task.GetAwaiter().GetResult();
            }

            // Otherwise, release the lock acquired when entering WriteAsync
            _writeLock.Release();

            return default;
        }

        private ValueTask<FlushResult> WriteCore<TMessage>(
            TMessage message,
            PipeWriter output,
            IRaidoProtocol protocol,
            CancellationToken cancellationToken) where TMessage : RaidoMessage
        {
            try
            {
                // We know that we are only writing this message to one receiver, so we can
                // write it without caching.
                protocol.WriteMessage(message, output);

                // check if there is actually a message encoded
                if (!output.CanGetUnflushedBytes || output.UnflushedBytes > 0)
                {
                    Log.SentMessage(_logger, message);
                }

                return output.FlushAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                CloseException = ex;
                Log.FailedWritingMessage(_logger, ex);

                AbortAllowReconnect();

                return new ValueTask<FlushResult>(new FlushResult(isCanceled: false, isCompleted: true));
            }
        }

        private async Task CompleteWriteAsync(ValueTask<FlushResult> task)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                CloseException = ex;
                Log.FailedWritingMessage(_logger, ex);

                AbortAllowReconnect();
            }
            finally
            {
                // Release the lock acquired when entering WriteAsync
                _writeLock.Release();
            }
        }

        private async Task WriteSlowAsync<TMessage>(
            TMessage message,
            bool ignoreAbort,
            IRaidoProtocol? protocolOverride,
            CancellationToken cancellationToken) where TMessage : RaidoMessage
        {
            // Failed to get the lock immediately when entering WriteAsync so await until it is available
            await _writeLock.WaitAsync(cancellationToken);

            try
            {
                if (IsReconnecting())
                {
                    Log.WriteWhileReconnecting(_logger, ConnectionId);
                    throw new RaidoConnectionReconnectingException(ConnectionId);
                }

                if (_connectionAborted && !ignoreAbort)
                {
                    return;
                }

                await WriteCore(message, GetPhysicalTransport().Transport.Output, protocolOverride ?? Protocol, cancellationToken);
            }
            catch (RaidoConnectionReconnectingException)
            {
                throw;
            }
            catch (Exception ex)
            {
                CloseException = ex;
                Log.FailedWritingMessage(_logger, ex);
                AbortAllowReconnect();
            }
            finally
            {
                _writeLock.Release();
            }
        }

        /// <summary>
        /// Aborts the connection.
        /// </summary>
        public virtual void Abort()
        {
            RaidoPhysicalTransport? physicalTransport;
            bool queueAbort;

            lock (_lifecycleLock)
            {
                if (_lifecycleState == RaidoConnectionLifecycleState.Closed)
                {
                    return;
                }

                _statefulReconnectEnabled = false;
                _lifecycleState = RaidoConnectionLifecycleState.Closed;
                _connectionAborted = true;
                _graceTimer?.Dispose();
                _graceTimer = null;
                _rebindTcs.TrySetResult(false);
                _transportHandoffCallback = null;
                physicalTransport = DetachPhysicalUnsafe();
                queueAbort = !_connectionAbortedTokenSource.IsCancellationRequested;
                Log.TerminalClosed(_logger, ConnectionId);
            }

            CancelPhysicalConnection(physicalTransport);

            if (queueAbort)
            {
                // We fire and forget since this can trigger user code to run.
                ThreadPool.QueueUserWorkItem(_abortedCallback, this);
            }
        }

        /// <summary>
        /// Aborts the current physical transport while retaining the logical connection
        /// when this connection has explicitly enabled stateful reconnect.
        /// </summary>
        internal void AbortAllowReconnect()
        {
            RaidoPhysicalTransport? physicalTransport;
            bool queueAbort = false;

            lock (_lifecycleLock)
            {
                if (_lifecycleState == RaidoConnectionLifecycleState.Closed ||
                    _lifecycleState == RaidoConnectionLifecycleState.Reconnecting)
                {
                    return;
                }

                physicalTransport = DetachPhysicalUnsafe();
                if (!_statefulReconnectEnabled)
                {
                    _lifecycleState = RaidoConnectionLifecycleState.Closed;
                    _connectionAborted = true;
                    _rebindTcs.TrySetResult(false);
                    queueAbort = !_connectionAbortedTokenSource.IsCancellationRequested;
                    Log.TerminalClosed(_logger, ConnectionId);
                }
                else
                {
                    StartReconnectUnsafe();
                }
            }

            CancelPhysicalConnection(physicalTransport);

            if (queueAbort)
            {
                ThreadPool.QueueUserWorkItem(_abortedCallback, this);
            }
        }

        // Used by the HubConnectionHandler only
        internal Task AbortAsync()
        {
            Abort();

            // Acquire lock to make sure all writes are completed
            if (!_writeLock.Wait(0))
            {
                return AbortAsyncSlow();
            }

            _writeLock.Release();
            return _abortCompletedTcs.Task;
        }

        private async Task AbortAsyncSlow()
        {
            await _writeLock.WaitAsync();
            _writeLock.Release();
            await _abortCompletedTcs.Task;
        }

        internal ValueTask<bool> TryRebindAsync(RaidoConnectionContext replacement) =>
            TryRebindAsync(replacement, replacementProtocol: null);

        internal async ValueTask<bool> TryRebindAsync(RaidoConnectionContext replacement, IRaidoProtocol? replacementProtocol)
        {
            ArgumentNullException.ThrowIfNull(replacement);
            if (ReferenceEquals(this, replacement))
            {
                Log.RebindRejected(_logger, ConnectionId);
                return false;
            }

            // Use the same order as message dispatch (dispatch gate, then write lock) so a
            // rebind cannot deadlock with an in-flight handler that is writing a response.
            await _dispatchLock.WaitAsync().ConfigureAwait(false);
            var rebound = false;
            try
            {
                await _writeLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    lock (_lifecycleLock)
                    {
                        if (_lifecycleState != RaidoConnectionLifecycleState.Reconnecting || _physicalTransport is not null)
                        {
                            Log.RebindRejected(_logger, ConnectionId);
                            return false;
                        }

                        var replacementTransport = replacement.DetachPhysicalForRebind();
                        if (replacementTransport is null)
                        {
                            Log.RebindRejected(_logger, ConnectionId);
                            return false;
                        }

                        if (replacementTransport.ConnectionClosed.IsCancellationRequested)
                        {
                            Log.RebindRejected(_logger, ConnectionId);
                            return false;
                        }

                        AttachPhysicalUnsafe(replacementTransport);
                        if (!ReferenceEquals(_physicalTransport, replacementTransport))
                        {
                            Log.RebindRejected(_logger, ConnectionId);
                            return false;
                        }

                        if (replacementProtocol is not null)
                        {
                            Protocol = replacementProtocol;
                        }

                        _lifecycleState = RaidoConnectionLifecycleState.Connected;
                        _graceTimer?.Dispose();
                        _graceTimer = null;
                        _rebindTcs.TrySetResult(true);
                        _rebindTcs = NewRebindTcs();
                        Log.RebindSucceeded(_logger, ConnectionId, _physicalConnectionId);
                        rebound = true;
                    }
                }
                finally
                {
                    _writeLock.Release();
                }
            }
            finally
            {
                _dispatchLock.Release();
            }

            if (!rebound)
            {
                return false;
            }

            var reconnectedCallback = TakeReconnectedCallback();
            if (reconnectedCallback is not null)
            {
                try
                {
                    await reconnectedCallback(Output).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    CloseException = ex;
                    AbortAllowReconnect();
                    throw;
                }
            }

            return true;
        }

        /// <summary>
        /// Waits until the retained logical connection is rebound or reaches terminal closure.
        /// </summary>
        public Task<bool> WaitForRebindOrCloseAsync() => _rebindTcs.Task;

        /// <summary>
        /// Waits until terminal cleanup has started for this logical connection.
        /// </summary>
        public Task WaitForTerminationAsync() => _abortCompletedTcs.Task;

        private void EnableStatefulReconnect()
        {
            lock (_lifecycleLock)
            {
                if (_statefulReconnectSupported && _lifecycleState != RaidoConnectionLifecycleState.Closed)
                {
                    _statefulReconnectEnabled = true;
                }
            }
        }

        private void DisableStatefulReconnect()
        {
            bool queueAbort;
            RaidoPhysicalTransport? physicalTransport;

            lock (_lifecycleLock)
            {
                _statefulReconnectEnabled = false;
                if (_lifecycleState != RaidoConnectionLifecycleState.Reconnecting)
                {
                    return;
                }

                _lifecycleState = RaidoConnectionLifecycleState.Closed;
                _connectionAborted = true;
                _graceTimer?.Dispose();
                _graceTimer = null;
                _rebindTcs.TrySetResult(false);
                physicalTransport = DetachPhysicalUnsafe();
                queueAbort = !_connectionAbortedTokenSource.IsCancellationRequested;
                Log.TerminalClosed(_logger, ConnectionId);
            }

            CancelPhysicalConnection(physicalTransport);

            if (queueAbort)
            {
                ThreadPool.QueueUserWorkItem(_abortedCallback, this);
            }
        }

        private void StartReconnectUnsafe()
        {
            _lifecycleState = RaidoConnectionLifecycleState.Reconnecting;
            _rebindTcs = NewRebindTcs();
            Log.ReconnectWindowStarted(_logger, ConnectionId, _statefulReconnectGracePeriod);
            _graceTimer = _timeProvider.CreateTimer(static state => ((RaidoConnectionContext)state!).GraceExpired(), this,
                _statefulReconnectGracePeriod, Timeout.InfiniteTimeSpan);
        }

        private static TaskCompletionSource<bool> NewRebindTcs() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private sealed class StatefulReconnectFeature(RaidoConnectionContext connection) : IRaidoStatefulReconnectFeature
        {
            public void EnableReconnect() => connection.EnableStatefulReconnect();

            public void DisableReconnect() => connection.DisableStatefulReconnect();

            public void OnReconnected(Func<PipeWriter, Task> callback) => connection.SetReconnectedCallback(callback);
        }

        private sealed class TransportHandoffFeature(RaidoConnectionContext connection) : IRaidoTransportHandoffFeature
        {
            public void OnTransportReady(Func<PipeWriter, Task> callback) => connection.SetTransportHandoffCallback(callback);
        }

        private void SetReconnectedCallback(Func<PipeWriter, Task> callback)
        {
            ArgumentNullException.ThrowIfNull(callback);
            lock (_lifecycleLock)
            {
                if (_reconnectedCallback is not null)
                {
                    throw new InvalidOperationException("Only one reconnect callback may be registered.");
                }

                _reconnectedCallback = callback;
            }
        }

        private void SetTransportHandoffCallback(Func<PipeWriter, Task> callback)
        {
            ArgumentNullException.ThrowIfNull(callback);
            lock (_lifecycleLock)
            {
                if (_transportHandoffCallback is not null || _transportWasHandedOff || _lifecycleState != RaidoConnectionLifecycleState.Connected)
                {
                    throw new InvalidOperationException("A transport handoff can only be registered once while connected.");
                }

                _transportHandoffCallback = callback;
            }
        }

        internal bool TryTakeTransportHandoff(out Func<PipeWriter, Task>? callback)
        {
            lock (_lifecycleLock)
            {
                callback = _transportHandoffCallback;
                _transportHandoffCallback = null;
                return callback is not null;
            }
        }

        private Func<PipeWriter, Task>? TakeReconnectedCallback()
        {
            lock (_lifecycleLock)
            {
                var callback = _reconnectedCallback;
                _reconnectedCallback = null;
                return callback;
            }
        }

        internal bool TransportWasHandedOff
        {
            get
            {
                lock (_lifecycleLock)
                {
                    return _transportWasHandedOff;
                }
            }
        }

        private ConnectionContext GetPhysicalConnection() => GetPhysicalTransport().Connection;

        private RaidoPhysicalTransport GetPhysicalTransport()
        {
            lock (_lifecycleLock)
            {
                return _physicalTransport ?? throw new RaidoConnectionReconnectingException(ConnectionId);
            }
        }

        private bool IsReconnecting()
        {
            lock (_lifecycleLock)
            {
                return _lifecycleState == RaidoConnectionLifecycleState.Reconnecting;
            }
        }

        private void AttachPhysicalUnsafe(RaidoPhysicalTransport transport)
        {
            _closedRegistration.Dispose();
            _closedRequestedRegistration?.Dispose();
            _closedRequestedRegistration = null;
            _physicalTransport = transport;
            _physicalConnectionId = transport.ConnectionId;
            _physicalGeneration++;
            var generation = _physicalGeneration;
            _physicalAbortedTokenSource = new CancellationTokenSource();
            _closedRegistration = transport.ConnectionClosed.Register(() => OnPhysicalClosed(generation));

            if (transport.Features.Get<IConnectionLifetimeNotificationFeature>() is IConnectionLifetimeNotificationFeature lifetimeNotification)
            {
                // This feature requests a terminal close, for example when authentication expires.
                _closedRequestedRegistration = lifetimeNotification.ConnectionClosedRequested.Register(Abort);
            }
        }

        private RaidoPhysicalTransport? DetachPhysicalUnsafe()
        {
            var transport = _physicalTransport;
            _physicalTransport = null;
            if (transport is not null)
            {
                _physicalAbortedTokenSource.Cancel();
                _physicalAbortedTokenSource.Dispose();
            }

            return transport;
        }

        private RaidoPhysicalTransport? DetachPhysicalForRebind()
        {
            lock (_lifecycleLock)
            {
                if (_lifecycleState != RaidoConnectionLifecycleState.Connected || _physicalTransport is null || _connectionAborted)
                {
                    return null;
                }

                var transport = DetachPhysicalUnsafe();
                _closedRegistration.Dispose();
                _closedRequestedRegistration?.Dispose();
                _closedRequestedRegistration = null;
                _transportWasHandedOff = true;
                return transport;
            }
        }

        private static void CancelPhysicalConnection(RaidoPhysicalTransport? transport)
        {
            if (transport is null)
            {
                return;
            }

            transport.Transport.Output.CancelPendingFlush();
            transport.Transport.Input.CancelPendingRead();
        }

        private void OnPhysicalClosed(long generation)
        {
            RaidoPhysicalTransport? physicalTransport;
            bool queueAbort = false;

            lock (_lifecycleLock)
            {
                if (_lifecycleState == RaidoConnectionLifecycleState.Closed ||
                    _physicalGeneration != generation ||
                    _physicalTransport is null)
                {
                    return;
                }

                var physicalConnectionId = _physicalConnectionId;
                physicalTransport = DetachPhysicalUnsafe();
                Log.TransportLost(_logger, ConnectionId, physicalConnectionId);
                if (!_statefulReconnectEnabled)
                {
                    _lifecycleState = RaidoConnectionLifecycleState.Closed;
                    _connectionAborted = true;
                    _rebindTcs.TrySetResult(false);
                    queueAbort = !_connectionAbortedTokenSource.IsCancellationRequested;
                    Log.TerminalClosed(_logger, ConnectionId);
                }
                else
                {
                    StartReconnectUnsafe();
                }
            }

            CancelPhysicalConnection(physicalTransport);

            if (queueAbort)
            {
                ThreadPool.QueueUserWorkItem(_abortedCallback, this);
            }
        }

        private void GraceExpired()
        {
            bool queueAbort = false;
            lock (_lifecycleLock)
            {
                if (_lifecycleState != RaidoConnectionLifecycleState.Reconnecting)
                {
                    return;
                }

                _lifecycleState = RaidoConnectionLifecycleState.Closed;
                _connectionAborted = true;
                _graceTimer = null;
                _rebindTcs.TrySetResult(false);
                queueAbort = !_connectionAbortedTokenSource.IsCancellationRequested;
                Log.ReconnectExpired(_logger, ConnectionId);
                Log.TerminalClosed(_logger, ConnectionId);
            }

            if (queueAbort)
            {
                ThreadPool.QueueUserWorkItem(_abortedCallback, this);
            }
        }

        internal void StartClientTimeout()
        {
            if (_clientTimeoutActive)
            {
                return;
            }

            _clientTimeoutActive = true;
            Features.Get<IConnectionHeartbeatFeature>()?.OnHeartbeat(state => ((RaidoConnectionContext)state).CheckClientTimeout(), this);
        }

        private void CheckClientTimeout()
        {
            if (Debugger.IsAttached || _connectionAborted || LifecycleState != RaidoConnectionLifecycleState.Connected)
            {
                return;
            }

            lock (_receiveMessageTimeoutLock)
            {
                if (_receivedMessageTimeoutEnabled)
                {
                    _receivedMessageElapsed = _timeProvider.GetElapsedTime(_receivedMessageTick);

                    if (_receivedMessageElapsed >= _clientTimeoutInterval)
                    {
                        CloseException ??=
                            new OperationCanceledException(
                                $"Client hasn't sent a message/ping within the configured {nameof(RaidoConnectionContextOptions.ClientTimeoutInterval)}.");
                        Log.ClientTimeout(_logger, _clientTimeoutInterval);
                        RaidoEventSource.Log.ConnectionTimedOut(ConnectionId);
                        AbortAllowReconnect();
                    }
                }
            }
        }

        private static void AbortConnection(object? state)
        {
            var connection = (RaidoConnectionContext)state!;

            try
            {
                connection._connectionAbortedTokenSource.Cancel();
            }
            catch (Exception ex)
            {
                Log.AbortFailed(connection._logger, ex);
            }
            finally
            {
                _ = InnerAbortConnection(connection);
            }

            static async Task InnerAbortConnection(RaidoConnectionContext connection)
            {
                // We lock to make sure all writes are done before triggering the completion of the pipe
                await connection._writeLock.WaitAsync();
                try
                {
                    // Communicate the fact that we're finished triggering abort callbacks
                    // HubOnDisconnectedAsync is waiting on this to complete the Pipe
                    connection._abortCompletedTcs.TrySetResult();
                }
                finally
                {
                    connection._writeLock.Release();
                }

            }
        }

        internal void BeginClientTimeout()
        {
            lock (_receiveMessageTimeoutLock)
            {
                _receivedMessageTimeoutEnabled = true;
                _receivedMessageTick = _timeProvider.GetTimestamp();
            }
        }

        internal void StopClientTimeout()
        {
            lock (_receiveMessageTimeoutLock)
            {
                // we received a message so stop the timer and reset it
                // it will resume after the message has been processed
                _receivedMessageElapsed = TimeSpan.Zero;
                _receivedMessageTick = 0;
                _receivedMessageTimeoutEnabled = false;
            }
        }

        private void KeepAliveTick()
        {
            var currentTime = _timeProvider.GetTimestamp();
            var elapsed = _timeProvider.GetElapsedTime(Volatile.Read(ref _lastSendTick), currentTime);

            // Implements the keep-alive tick behavior
            // Each tick, we check if the time since the last send is larger than the keep alive duration (in ticks).
            // If it is, we send a ping frame, if not, we no-op on this tick. This means that in the worst case, the
            // true "ping rate" of the server could be (_hubOptions.KeepAliveInterval + HubEndPoint.KeepAliveTimerInterval),
            // because if the interval elapses right after the last tick of this timer, it won't be detected until the next tick.

            if (elapsed > _keepAliveInterval)
            {
                // Haven't sent a message for the entire keep-alive duration, so send a ping.
                // If the transport channel is full, this will fail, but that's OK because
                // adding a Ping message when the transport is full is unnecessary since the
                // transport is still in the process of sending frames.
                _ = TryWritePingAsync().Preserve();

                // We only update the timestamp here, because updating on each sent message is bad for performance
                // There can be a lot of sent messages per 15 seconds
                Volatile.Write(ref _lastSendTick, currentTime);
            }
        }

        // Don't wait for the lock, if it returns false that means someone wrote to the connection
        // and we don't need to send a ping anymore
        private ValueTask TryWritePingAsync() => !_writeLock.Wait(0) ? default : new ValueTask(TryWritePingSlowAsync());

        private async Task TryWritePingSlowAsync()
        {
            try
            {
                if (_connectionAborted || IsReconnecting())
                {
                    return;
                }

                var pingMessage = Protocol.GetMessageBytes(PingMessage.Instance);
                await GetPhysicalConnection().Transport.Output.WriteAsync(pingMessage);

                Log.SentPing(_logger);
            }
            catch (Exception ex)
            {
                CloseException = ex;
                Log.FailedWritingMessage(_logger, ex);
                AbortAllowReconnect();
            }
            finally
            {
                _writeLock.Release();
            }
        }

        internal void Cleanup()
        {
            _closedRegistration.Dispose();
            _closedRequestedRegistration?.Dispose();
            _graceTimer?.Dispose();
            _physicalAbortedTokenSource.Dispose();
        }

        private static class Log
        {
            private static readonly Action<ILogger, Exception?> _sentPing =
                LoggerMessage.Define(LogLevel.Trace, new EventId(1, "SentPing"), "Sent a ping message to the client.");

            private static readonly Action<ILogger, string, Exception?> _sentMessage =
                LoggerMessage.Define<string>(LogLevel.Trace, new EventId(2, "SentMessage"), "Sent a {Message} to the client.");

            private static readonly Action<ILogger, Exception> _failedWritingMessage = LoggerMessage.Define(LogLevel.Debug,
                new EventId(3, "FailedWritingMessage"),
                "Failed writing message. Aborting connection.");

            private static readonly Action<ILogger, Exception> _abortFailed =
                LoggerMessage.Define(LogLevel.Trace, new EventId(4, "AbortFailed"), "Abort callback failed.");

            private static readonly Action<ILogger, int, Exception?> _clientTimeout = LoggerMessage.Define<int>(LogLevel.Debug,
                new EventId(5, "ClientTimeout"),
                "Client timeout ({ClientTimeout}ms) elapsed without receiving a message from the client. Closing connection.");
            private static readonly Action<ILogger, string, string, Exception?> _transportLost = LoggerMessage.Define<string, string>(
                LogLevel.Debug, new EventId(6, "TransportLost"),
                "Physical connection '{PhysicalConnectionId}' was lost for logical connection '{ConnectionId}'.");
            private static readonly Action<ILogger, string, int, Exception?> _reconnectWindowStarted = LoggerMessage.Define<string, int>(
                LogLevel.Debug, new EventId(7, "ReconnectWindowStarted"),
                "Logical connection '{ConnectionId}' entered a {GracePeriodMs}ms reconnect grace window.");
            private static readonly Action<ILogger, string, string, Exception?> _rebindSucceeded = LoggerMessage.Define<string, string>(
                LogLevel.Debug, new EventId(8, "RebindSucceeded"),
                "Logical connection '{ConnectionId}' rebound to physical connection '{PhysicalConnectionId}'.");
            private static readonly Action<ILogger, string, Exception?> _rebindRejected = LoggerMessage.Define<string>(
                LogLevel.Debug, new EventId(9, "RebindRejected"),
                "A replacement transport was rejected for logical connection '{ConnectionId}'.");
            private static readonly Action<ILogger, string, Exception?> _reconnectExpired = LoggerMessage.Define<string>(
                LogLevel.Debug, new EventId(10, "ReconnectExpired"), "Reconnect grace expired for logical connection '{ConnectionId}'.");
            private static readonly Action<ILogger, string, Exception?> _terminalClosed = LoggerMessage.Define<string>(
                LogLevel.Debug, new EventId(11, "TerminalClosed"), "Logical connection '{ConnectionId}' entered terminal closure.");
            private static readonly Action<ILogger, string, Exception?> _writeWhileReconnecting = LoggerMessage.Define<string>(
                LogLevel.Debug, new EventId(12, "WriteWhileReconnecting"),
                "A write was rejected while logical connection '{ConnectionId}' has no active physical transport.");

            public static void SentPing(ILogger logger) => _sentPing(logger, null);

            public static void SentMessage(ILogger logger, RaidoMessage message) => _sentMessage(logger, message.GetType().Name, null);

            public static void FailedWritingMessage(ILogger logger, Exception exception) => _failedWritingMessage(logger, exception);

            public static void AbortFailed(ILogger logger, Exception exception) => _abortFailed(logger, exception);

            public static void ClientTimeout(ILogger logger, TimeSpan timeout) => _clientTimeout(logger, (int)timeout.TotalMilliseconds, null);

            public static void TransportLost(ILogger logger, string connectionId, string physicalConnectionId) =>
                _transportLost(logger, physicalConnectionId, connectionId, null);

            public static void ReconnectWindowStarted(ILogger logger, string connectionId, TimeSpan gracePeriod) =>
                _reconnectWindowStarted(logger, connectionId, (int)gracePeriod.TotalMilliseconds, null);

            public static void RebindSucceeded(ILogger logger, string connectionId, string physicalConnectionId) =>
                _rebindSucceeded(logger, connectionId, physicalConnectionId, null);

            public static void RebindRejected(ILogger logger, string connectionId) => _rebindRejected(logger, connectionId, null);

            public static void ReconnectExpired(ILogger logger, string connectionId) => _reconnectExpired(logger, connectionId, null);

            public static void TerminalClosed(ILogger logger, string connectionId) => _terminalClosed(logger, connectionId, null);

            public static void WriteWhileReconnecting(ILogger logger, string connectionId) => _writeWhileReconnecting(logger, connectionId, null);
        }
    }
}
