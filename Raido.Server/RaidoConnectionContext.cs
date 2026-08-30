using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private static readonly TimeSpan MaxSupportedReconnectTimeout =
            TimeSpan.FromMilliseconds(uint.MaxValue - 1L);

        private readonly TaskCompletionSource _abortCompletedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ConnectionContext _connection;
        private readonly CancellationTokenSource _connectionAbortedTokenSource = new();

        private readonly ILogger _logger;
        private readonly Lock _reconnectLock = new();
        private readonly Lock _receiveMessageTimeoutLock = new();
        private readonly TimeProvider _timeProvider;
        private readonly SemaphoreSlim _writeLock = new(1);
        private readonly TimeSpan _statefulReconnectTimeout;
        private CancellationTokenRegistration _closedRegistration;
        private CancellationTokenRegistration? _closedRequestedRegistration;
        private ConnectionContext? _currentConnection;
        private ConnectionContext? _detachedConnection;
        private ConnectionContext? _reconnectCandidate;
        private IRaidoProtocol? _reconnectCandidateProtocol;
        private TaskCompletionSource<bool>? _reconnectWaiter;
        private long? _reconnectWindowStartTimestamp;
        private ClaimsPrincipal? _user;

        private volatile bool _clientTimeoutActive;
        private volatile bool _connectionAborted;
        private bool _abortCallbackQueued;
        private bool _reconnectEnabled;
        private long _lastSendTick;
        private TimeSpan _receivedMessageElapsed;
        private bool _receivedMessageTimeoutEnabled;
        private long _receivedMessageTick;
        private RaidoConnectionContext? _reconnectHandoffTarget;
        private volatile bool _physicalTransportAdopted;

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
        public virtual string ConnectionId => _connection.ConnectionId;

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
        public virtual IFeatureCollection Features => _connection.Features;

        /// <summary>
        /// Gets a key/value collection that can be used to share data within the scope of this connection.
        /// </summary>
        public virtual IDictionary<object, object?> Items => _connection.Items;

        /// <summary>
        /// Gets the input pipe for the connection.
        /// </summary>
        public virtual PipeReader Input => GetRequiredCurrentConnection().Transport.Input;

        /// <summary>
        /// Gets the output pipe for the connection.
        /// </summary>
        public virtual PipeWriter Output => GetRequiredCurrentConnection().Transport.Output;

        /// <summary>
        /// Gets the local endpoint for the connection.
        /// </summary>
        public virtual IPEndPoint? LocalEndPoint => GetCurrentConnection()?.LocalEndPoint as IPEndPoint;

        /// <summary>
        /// Gets the remote endpoint for the connection.
        /// </summary>
        public virtual IPEndPoint? RemoteEndPoint => GetCurrentConnection()?.RemoteEndPoint as IPEndPoint;


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
            : this(connection, contextOptions, loggerFactory, TimeProvider.System)
        {
        }

        internal RaidoConnectionContext(
            ConnectionContext connection,
            RaidoConnectionContextOptions contextOptions,
            ILoggerFactory loggerFactory,
            TimeProvider timeProvider)
        {
            if (contextOptions.StatefulReconnectEnabled &&
                (contextOptions.StatefulReconnectTimeout <= TimeSpan.Zero ||
                 contextOptions.StatefulReconnectTimeout > MaxSupportedReconnectTimeout))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(contextOptions.StatefulReconnectTimeout),
                    contextOptions.StatefulReconnectTimeout,
                    "Stateful reconnect timeout must be greater than zero and within the supported .NET timer range.");
            }

            _connection = connection;
            _logger = loggerFactory.CreateLogger<RaidoConnectionContext>();

            _clientTimeoutInterval = contextOptions.ClientTimeoutInterval;
            _keepAliveInterval = contextOptions.KeepAliveInterval;
            _statefulReconnectTimeout = contextOptions.StatefulReconnectTimeout;
            _reconnectEnabled = contextOptions.StatefulReconnectEnabled;
            _currentConnection = connection;
            ConnectionAbortedToken = _connectionAbortedTokenSource.Token;

            RaidoCallerContext = new DefaultRaidoCallerContext(this);

            _timeProvider = timeProvider;
            _lastSendTick = _timeProvider.GetTimestamp();

            RegisterPhysicalCallbacks(connection, registerHeartbeat: false, out var closedRegistration, out var closedRequestedRegistration);

            lock (_reconnectLock)
            {
                if (!_connectionAborted && ReferenceEquals(connection, _currentConnection))
                {
                    _closedRegistration = closedRegistration;
                    closedRegistration = default;
                    _closedRequestedRegistration = closedRequestedRegistration;
                    closedRequestedRegistration = null;
                }
                else if (!_connectionAborted &&
                    _currentConnection is null &&
                    ReferenceEquals(connection, _detachedConnection) &&
                    _reconnectEnabled &&
                    _reconnectWaiter is not null &&
                    !_reconnectWaiter.Task.IsCompleted)
                {
                    Debug.Assert(_closedRequestedRegistration is null, "The constructor must not already own a close-request registration while publishing detached ownership.");
                    _closedRequestedRegistration = closedRequestedRegistration;
                    closedRequestedRegistration = null;
                }
            }

            closedRegistration.Dispose();
            closedRequestedRegistration?.Dispose();
        }

        internal Task OnConnectedAsync()
        {
            if (GetCurrentConnection() is ConnectionContext connection)
            {
                RegisterHeartbeatCallbacks(connection);
            }

            StartTimestamp = _timeProvider.GetTimestamp();

            return Task.CompletedTask;
        }

        private void RegisterPhysicalCallbacks(
            ConnectionContext connection,
            bool registerHeartbeat,
            out CancellationTokenRegistration closedRegistration,
            out CancellationTokenRegistration? closedRequestedRegistration)
        {
            closedRegistration = default;
            closedRequestedRegistration = null;

            var localClosedRegistration = connection.ConnectionClosed.Register(() => OnPhysicalConnectionClosed(connection));
            CancellationTokenRegistration? localClosedRequestedRegistration = null;
            try
            {
                // This feature is used by HttpConnectionManager to close a connection with a non-errored closed message.
                localClosedRequestedRegistration = connection.Features.Get<IConnectionLifetimeNotificationFeature>() is IConnectionLifetimeNotificationFeature lifetimeNotification
                    ? lifetimeNotification.ConnectionClosedRequested.Register(() => OnConnectionClosedRequested(connection))
                    : null;

                if (registerHeartbeat)
                {
                    RegisterHeartbeatCallbacks(connection);
                }

                closedRegistration = localClosedRegistration;
                closedRequestedRegistration = localClosedRequestedRegistration;
            }
            catch
            {
                localClosedRegistration.Dispose();
                localClosedRequestedRegistration?.Dispose();
                throw;
            }
        }

        private void RegisterHeartbeatCallbacks(ConnectionContext connection)
        {
            if (connection.Features.Get<IConnectionInherentKeepAliveFeature>()?.HasInherentKeepAlive != true)
            {
                connection.Features.Get<IConnectionHeartbeatFeature>()?.OnHeartbeat(_ => KeepAliveTick(connection), this);
            }

            if (_clientTimeoutActive)
            {
                RegisterClientTimeoutHeartbeat(connection);
            }
        }

        private void RegisterClientTimeoutHeartbeat(ConnectionContext connection) =>
            connection.Features.Get<IConnectionHeartbeatFeature>()?.OnHeartbeat(_ => CheckClientTimeoutForConnection(connection), this);

        private bool IsCurrentConnection(ConnectionContext connection)
        {
            lock (_reconnectLock)
            {
                return ReferenceEquals(connection, _currentConnection);
            }
        }

        /// <summary>
        /// Writes a message to the connection.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="message">The message to write.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the write operation.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous write operation.</returns>
        public virtual ValueTask WriteAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : RaidoMessage =>
            WriteAsync<TMessage>(message, ignoreAbort: false, cancellationToken);

        internal ValueTask WriteAsync<TMessage>(TMessage message, bool ignoreAbort, CancellationToken cancellationToken = default)
            where TMessage : RaidoMessage
        {
            // Try to grab the lock synchronously, if we fail, go to the slower path
#pragma warning disable CA2016 // This will always finish synchronously so we do not need to both with cancel
            if (!_writeLock.Wait(0))
#pragma warning restore CA2016
            {
                return new ValueTask(WriteSlowAsync(message, ignoreAbort, cancellationToken));
            }

            var currentConnection = GetCurrentConnection();
            if (currentConnection is null || (_connectionAborted && !ignoreAbort))
            {
                _writeLock.Release();
                return default;
            }

            // This method should never throw synchronously
            var task = WriteCore(currentConnection, message, cancellationToken);

            // The write didn't complete synchronously so await completion
            if (!task.IsCompletedSuccessfully)
            {
                return new ValueTask(CompleteWriteAndReleaseAsync(currentConnection, task, cancellationToken));
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

        /// <summary>
        /// Writes a handshake message directly to this context's current physical transport.
        /// Unlike the normal write path, this reports physical write/flush failure to the
        /// reconnect handoff instead of absorbing it.
        /// </summary>
        internal async ValueTask<bool> WriteHandshakeAsync(RaidoMessage message, CancellationToken cancellationToken = default)
        {
            try
            {
                await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            ConnectionContext? connection = null;
            try
            {
                connection = GetCurrentConnection();
                if (connection is null || _connectionAborted || _physicalTransportAdopted)
                {
                    return false;
                }

                var bytes = Protocol.GetMessageBytes(message);
                if (bytes.IsEmpty)
                {
                    return false;
                }

                var result = await connection.Transport.Output.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                if (result.IsCanceled || result.IsCompleted || cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                Log.SentMessage(_logger, message);
                return true;
            }
            catch (OperationCanceledException ex)
            {
                if (connection is not null)
                {
                    HandleTransportFailure(connection, ex);
                }

                return false;
            }
            catch (IOException ex)
            {
                if (connection is not null)
                {
                    HandleTransportFailure(connection, ex);
                }

                return false;
            }
            catch (ObjectDisposedException ex)
            {
                if (connection is not null)
                {
                    HandleTransportFailure(connection, ex);
                }

                return false;
            }
            catch (Exception ex)
            {
                if (connection is not null)
                {
                    TryAbortForConnection(connection, ex);
                }

                return false;
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private ValueTask<FlushResult> WriteCore<TMessage>(ConnectionContext connection, TMessage message, CancellationToken cancellationToken)
            where TMessage : RaidoMessage
        {
            PipeWriter output;
            try
            {
                // We know that we are only writing this message to one receiver, so we can
                // write it without caching.
                output = connection.Transport.Output;
                Protocol.WriteMessage(message, output);
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                TryAbortForConnection(connection, ex);
                return new ValueTask<FlushResult>(new FlushResult(isCanceled: false, isCompleted: true));
            }

            try
            {
                // check if there is actually a message encoded
                if (!output.CanGetUnflushedBytes || output.UnflushedBytes > 0)
                {
                    Log.SentMessage(_logger, message);
                }
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                TryAbortForConnection(connection, ex);
                return new ValueTask<FlushResult>(new FlushResult(isCanceled: false, isCompleted: true));
            }

            try
            {
                return output.FlushAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return new ValueTask<FlushResult>(Task.FromCanceled<FlushResult>(cancellationToken));
            }
            catch (OperationCanceledException ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                HandleTransportFailure(connection, ex);
            }
            catch (IOException ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                HandleTransportFailure(connection, ex);
            }
            catch (ObjectDisposedException ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                HandleTransportFailure(connection, ex);
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                TryAbortForConnection(connection, ex);
            }

            return new ValueTask<FlushResult>(new FlushResult(isCanceled: false, isCompleted: true));
        }

        private async Task CompleteWriteAndReleaseAsync(ConnectionContext connection, ValueTask<FlushResult> task, CancellationToken cancellationToken)
        {
            try
            {
                await CompleteWriteAsync(connection, task, cancellationToken);
            }
            finally
            {
                // Release the lock acquired when entering WriteAsync
                _writeLock.Release();
            }
        }

        private async Task CompleteWriteAsync(ConnectionContext connection, ValueTask<FlushResult> task, CancellationToken cancellationToken)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                HandleTransportFailure(connection, ex);
            }
            catch (IOException ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                HandleTransportFailure(connection, ex);
            }
            catch (ObjectDisposedException ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                HandleTransportFailure(connection, ex);
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                TryAbortForConnection(connection, ex);
            }
        }

        private async Task WriteSlowAsync<TMessage>(TMessage message, bool ignoreAbort, CancellationToken cancellationToken) where TMessage : RaidoMessage
        {
            // Failed to get the lock immediately when entering WriteAsync so await until it is available
            await _writeLock.WaitAsync(cancellationToken);
            ConnectionContext? currentConnection = null;

            try
            {
                if (_connectionAborted && !ignoreAbort)
                {
                    return;
                }

                currentConnection = GetCurrentConnection();
                if (currentConnection is null)
                {
                    return;
                }

                await CompleteWriteAsync(currentConnection, WriteCore(currentConnection, message, cancellationToken), cancellationToken);
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
            ConnectionContext? currentConnection = null;
            CancellationTokenRegistration closedRegistration = default;
            CancellationTokenRegistration? closedRequestedRegistration = null;
            var queueAbortCallback = false;

            lock (_reconnectLock)
            {
                TryTransitionToTerminalLocked(
                    expectedConnection: null,
                    expectedWaiter: null,
                    exception: null,
                    out currentConnection,
                    out closedRegistration,
                    out closedRequestedRegistration,
                    out queueAbortCallback);
            }

            CompleteTerminalTransition(currentConnection, closedRegistration, closedRequestedRegistration, queueAbortCallback);
        }

        /// <summary>
        /// Attempts to publish a replacement physical transport for this connection.
        /// </summary>
        /// <param name="replacement">The replacement physical transport.</param>
        /// <returns><see langword="true"/> when the replacement was published; otherwise, <see langword="false"/>.</returns>
        public bool TryReconnect(ConnectionContext replacement)
        {
            ArgumentNullException.ThrowIfNull(replacement);

            TaskCompletionSource<bool>? reconnectWaiter;
            ConnectionContext detachedConnection;
            CancellationToken? detachedCloseRequestedToken;
            lock (_reconnectLock)
            {
                if (_connectionAborted || !_reconnectEnabled || _currentConnection is not null ||
                    _reconnectWaiter is not TaskCompletionSource<bool> waiter || waiter.Task.IsCompleted ||
                    _detachedConnection is not ConnectionContext currentDetachedConnection ||
                    _reconnectCandidate is not null)
                {
                    return false;
                }

                reconnectWaiter = waiter;
                detachedConnection = currentDetachedConnection;
                detachedCloseRequestedToken = detachedConnection.Features.Get<IConnectionLifetimeNotificationFeature>()?.ConnectionClosedRequested;
            }

            CancellationTokenRegistration closedRegistration = default;
            CancellationTokenRegistration? closedRequestedRegistration = null;
            RegisterPhysicalCallbacks(replacement, registerHeartbeat: true, out closedRegistration, out closedRequestedRegistration);

            var closedRequestedToken = replacement.Features.Get<IConnectionLifetimeNotificationFeature>()?.ConnectionClosedRequested;
            var published = false;
            var terminal = false;
            ConnectionContext? terminalConnection = null;
            CancellationTokenRegistration terminalClosedRegistration = default;
            CancellationTokenRegistration? terminalClosedRequestedRegistration = null;
            CancellationTokenRegistration? obsoleteClosedRequestedRegistration = null;
            var queueAbortCallback = false;

            lock (_reconnectLock)
            {
                var reconnectWindowIsCurrent = !_connectionAborted && _reconnectEnabled && _currentConnection is null &&
                    ReferenceEquals(detachedConnection, _detachedConnection) &&
                    ReferenceEquals(reconnectWaiter, _reconnectWaiter) && !reconnectWaiter.Task.IsCompleted &&
                    _reconnectCandidate is null;

                if (reconnectWindowIsCurrent && detachedCloseRequestedToken.GetValueOrDefault().IsCancellationRequested)
                {
                    terminal = TryTransitionToTerminalLocked(
                        expectedConnection: null,
                        expectedWaiter: reconnectWaiter,
                        exception: null,
                        out terminalConnection,
                        out terminalClosedRegistration,
                        out terminalClosedRequestedRegistration,
                        out queueAbortCallback);
                }
                else if (reconnectWindowIsCurrent && !IsReconnectWindowExpiredLocked() &&
                    !replacement.ConnectionClosed.IsCancellationRequested &&
                    !closedRequestedToken.GetValueOrDefault().IsCancellationRequested)
                {
                    obsoleteClosedRequestedRegistration = _closedRequestedRegistration;
                    _currentConnection = replacement;
                    _detachedConnection = null;
                    _closedRegistration = closedRegistration;
                    _closedRequestedRegistration = closedRequestedRegistration;
                    _reconnectWaiter = null;
                    _reconnectWindowStartTimestamp = null;
                    CloseException = null;
                    reconnectWaiter.TrySetResult(true);
                    published = true;
                }
                else if (reconnectWindowIsCurrent && IsReconnectWindowExpiredLocked())
                {
                    terminal = TryTransitionToTerminalLocked(
                        expectedConnection: null,
                        expectedWaiter: reconnectWaiter,
                        exception: null,
                        out terminalConnection,
                        out terminalClosedRegistration,
                        out terminalClosedRequestedRegistration,
                        out queueAbortCallback);
                }
            }

            if (!published)
            {
                closedRegistration.Dispose();
                closedRequestedRegistration?.Dispose();
            }

            obsoleteClosedRequestedRegistration?.Dispose();

            if (terminal)
            {
                CompleteTerminalTransition(terminalConnection, terminalClosedRegistration, terminalClosedRequestedRegistration, queueAbortCallback);
            }

            return published;
        }

        /// <summary>
        /// Claims a replacement physical transport and completes the handshake on that transport.
        /// Publication remains part of the existing reconnect transition and occurs after the
        /// candidate reader has relinquished its consumed input boundary.
        /// </summary>
        public async ValueTask<bool> TryReconnectAsync(
            RaidoCallerContext replacement,
            IRaidoProtocol replacementProtocol,
            Func<ValueTask<bool>> completeHandshake)
        {
            ArgumentNullException.ThrowIfNull(replacement);
            ArgumentNullException.ThrowIfNull(replacementProtocol);
            ArgumentNullException.ThrowIfNull(completeHandshake);

            if (replacement is not IRaidoCallerContextTransport transport ||
                !transport.Connection.TryGetCurrentConnection(out var replacementPhysicalConnection))
            {
                return false;
            }

            TaskCompletionSource<bool>? reconnectWaiter = null;
            ConnectionContext? detachedConnection = null;
            var shouldTimeout = false;
            var claimCreated = false;
            lock (_reconnectLock)
            {
                if (_connectionAborted || !_reconnectEnabled || _currentConnection is not null ||
                    _reconnectWaiter is not TaskCompletionSource<bool> waiter || waiter.Task.IsCompleted ||
                    _detachedConnection is not ConnectionContext currentDetachedConnection ||
                    _reconnectCandidate is not null || transport.Connection.IsTerminal ||
                    replacementPhysicalConnection.ConnectionClosed.IsCancellationRequested)
                {
                    return false;
                }

                reconnectWaiter = waiter;
                detachedConnection = currentDetachedConnection;
                if (IsReconnectWindowExpiredLocked())
                {
                    shouldTimeout = true;
                }
                else
                {
                    _reconnectCandidate = replacementPhysicalConnection;
                    _reconnectCandidateProtocol = replacementProtocol;
                    claimCreated = transport.Connection.TrySetReconnectHandoffTarget(this);
                    if (!claimCreated)
                    {
                        _reconnectCandidate = null;
                        _reconnectCandidateProtocol = null;
                    }
                }
            }

            if (shouldTimeout || !claimCreated)
            {
                if (shouldTimeout)
                {
                    TimeoutReconnect(reconnectWaiter);
                }

                return false;
            }

            bool handshakeSucceeded;
            try
            {
                handshakeSucceeded = await completeHandshake().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                handshakeSucceeded = false;
            }

            if (!handshakeSucceeded)
            {
                transport.Connection.TryClearReconnectHandoffTarget(this);
                ClearReconnectCandidateIfOwner(replacementPhysicalConnection);
                return false;
            }

            shouldTimeout = false;
            lock (_reconnectLock)
            {
                var claimIsCurrent = ReferenceEquals(_reconnectCandidate, replacementPhysicalConnection);
                var windowIsCurrent = claimIsCurrent && !_connectionAborted && _reconnectEnabled &&
                    _currentConnection is null && ReferenceEquals(detachedConnection, _detachedConnection) &&
                    ReferenceEquals(reconnectWaiter, _reconnectWaiter) && !reconnectWaiter.Task.IsCompleted &&
                    !transport.Connection.IsTerminal;
                if (!windowIsCurrent)
                {
                    if (claimIsCurrent)
                    {
                        _reconnectCandidate = null;
                        _reconnectCandidateProtocol = null;
                    }

                    shouldTimeout = claimIsCurrent && IsReconnectWindowExpiredLocked();
                }
                else if (IsReconnectWindowExpiredLocked() || replacementPhysicalConnection.ConnectionClosed.IsCancellationRequested)
                {
                    _reconnectCandidate = null;
                    _reconnectCandidateProtocol = null;
                    shouldTimeout = IsReconnectWindowExpiredLocked();
                }
                else if (!ReferenceEquals(transport.Connection.GetReconnectHandoffTarget(), this))
                {
                    _reconnectCandidate = null;
                    _reconnectCandidateProtocol = null;
                }
                else
                {
                    return true;
                }
            }

            transport.Connection.TryClearReconnectHandoffTarget(this);
            if (shouldTimeout)
            {
                TimeoutReconnect(reconnectWaiter);
            }

            return false;
        }

        internal RaidoConnectionContext? GetReconnectHandoffTarget() => Volatile.Read(ref _reconnectHandoffTarget);

        internal bool TrySetReconnectHandoffTarget(RaidoConnectionContext target) =>
            !_physicalTransportAdopted &&
            ReferenceEquals(Interlocked.CompareExchange(ref _reconnectHandoffTarget, target, null), null);

        internal bool TryClearReconnectHandoffTarget(RaidoConnectionContext target) =>
            ReferenceEquals(Interlocked.CompareExchange(ref _reconnectHandoffTarget, null, target), target);

        internal bool TryMarkPhysicalTransportAdopted(RaidoConnectionContext target)
        {
            if (_physicalTransportAdopted || !ReferenceEquals(Volatile.Read(ref _reconnectHandoffTarget), target))
            {
                return false;
            }

            _physicalTransportAdopted = true;
            Interlocked.CompareExchange(ref _reconnectHandoffTarget, null, target);
            return true;
        }

        internal bool IsPhysicalTransportAdopted => _physicalTransportAdopted;

        internal bool TryPublishReconnect(RaidoConnectionContext replacement, ConnectionContext physicalConnection)
        {
            if (!ReferenceEquals(replacement.GetReconnectHandoffTarget(), this))
            {
                return false;
            }

            CancellationTokenRegistration closedRegistration;
            CancellationTokenRegistration? closedRequestedRegistration;
            try
            {
                RegisterPhysicalCallbacks(physicalConnection, registerHeartbeat: true, out closedRegistration, out closedRequestedRegistration);
            }
            catch
            {
                replacement.TryClearReconnectHandoffTarget(this);
                ClearReconnectCandidateIfOwner(physicalConnection);
                return false;
            }

            var published = false;
            var shouldTimeout = false;
            CancellationTokenRegistration? obsoleteClosedRequestedRegistration = null;
            TaskCompletionSource<bool>? reconnectWaiter = null;
            lock (_reconnectLock)
            {
                var claimIsCurrent = ReferenceEquals(_reconnectCandidate, physicalConnection);
                var windowIsCurrent = claimIsCurrent && !_connectionAborted && _reconnectEnabled &&
                    _currentConnection is null && _detachedConnection is not null &&
                    _reconnectWaiter is TaskCompletionSource<bool> waiter && !waiter.Task.IsCompleted;
                if (windowIsCurrent && IsReconnectWindowExpiredLocked())
                {
                    shouldTimeout = true;
                    reconnectWaiter = _reconnectWaiter;
                }
                else if (windowIsCurrent &&
                    !replacement.IsTerminal &&
                    !physicalConnection.ConnectionClosed.IsCancellationRequested &&
                    replacement.TryMarkPhysicalTransportAdopted(this))
                {
                    reconnectWaiter = _reconnectWaiter;
                    obsoleteClosedRequestedRegistration = _closedRequestedRegistration;
                    Protocol = _reconnectCandidateProtocol!;
                    _currentConnection = physicalConnection;
                    _detachedConnection = null;
                    _closedRegistration = closedRegistration;
                    _closedRequestedRegistration = closedRequestedRegistration;
                    _reconnectCandidate = null;
                    _reconnectCandidateProtocol = null;
                    _reconnectWaiter = null;
                    _reconnectWindowStartTimestamp = null;
                    CloseException = null;
                    reconnectWaiter!.TrySetResult(true);
                    published = true;
                }
                else if (claimIsCurrent)
                {
                    _reconnectCandidate = null;
                    _reconnectCandidateProtocol = null;
                }
            }

            if (!published)
            {
                closedRegistration.Dispose();
                closedRequestedRegistration?.Dispose();
                replacement.TryClearReconnectHandoffTarget(this);
            }

            obsoleteClosedRequestedRegistration?.Dispose();

            if (shouldTimeout && reconnectWaiter is not null)
            {
                TimeoutReconnect(reconnectWaiter);
            }

            return published;
        }

        internal void FailReconnectCandidate(ConnectionContext candidate) => ClearReconnectCandidateIfOwner(candidate);

        private bool ClearReconnectCandidateIfOwner(ConnectionContext candidate)
        {
            lock (_reconnectLock)
            {
                if (!ReferenceEquals(_reconnectCandidate, candidate))
                {
                    return false;
                }

                _reconnectCandidate = null;
                _reconnectCandidateProtocol = null;
                return true;
            }
        }

        internal bool IsTerminal => _connectionAborted;

        internal bool IsReconnectEnabled
        {
            get
            {
                lock (_reconnectLock)
                {
                    return _reconnectEnabled && !_connectionAborted;
                }
            }
        }

        internal bool TryGetCurrentConnection(out ConnectionContext connection)
        {
            lock (_reconnectLock)
            {
                if (_currentConnection is null)
                {
                    connection = null!;
                    return false;
                }

                connection = _currentConnection;
                return true;
            }
        }

        internal Task<bool> WaitForReconnectAsync() => WaitForReconnectAsync(_statefulReconnectTimeout);

        internal async Task<bool> WaitForReconnectAsync(TimeSpan timeout)
        {
            TaskCompletionSource<bool>? reconnectWaiter;
            TimeSpan remainingTimeout;
            lock (_reconnectLock)
            {
                if (_connectionAborted || !_reconnectEnabled)
                {
                    return false;
                }

                if (_currentConnection is not null)
                {
                    return true;
                }

                reconnectWaiter = _reconnectWaiter;
                if (reconnectWaiter is null)
                {
                    return false;
                }

                remainingTimeout = GetReconnectWaitTimeoutLocked(timeout);
            }

            if (remainingTimeout <= TimeSpan.Zero)
            {
                return TimeoutReconnect(reconnectWaiter);
            }

            try
            {
                return await reconnectWaiter.Task.WaitAsync(remainingTimeout).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                return TimeoutReconnect(reconnectWaiter);
            }
        }

        internal void OnPhysicalConnectionClosed(ConnectionContext connection) => TryDetachPhysicalConnection(connection, exception: null, out _);

        internal bool HandleTransportFailure(ConnectionContext connection, Exception exception)
        {
            var isCurrent = TryDetachPhysicalConnection(connection, exception, out var reconnecting);
            return !isCurrent || reconnecting;
        }

        private bool TryDetachPhysicalConnection(ConnectionContext connection, Exception? exception, out bool reconnecting)
        {
            CancellationTokenRegistration closedRegistration = default;
            CancellationTokenRegistration? closedRequestedRegistration = null;
            ConnectionContext? terminalConnection = null;
            CancellationTokenRegistration terminalClosedRegistration = default;
            CancellationTokenRegistration? terminalClosedRequestedRegistration = null;
            var terminal = false;
            var queueAbortCallback = false;

            lock (_receiveMessageTimeoutLock)
            {
                lock (_reconnectLock)
                {
                    if (!ReferenceEquals(connection, _currentConnection))
                    {
                        reconnecting = false;
                        return false;
                    }

                    reconnecting = !_connectionAborted && _reconnectEnabled;
                    if (reconnecting)
                    {
                        if (exception is not null)
                        {
                            CloseException = exception;
                        }

                        _currentConnection = null;
                        _detachedConnection = connection;
                        closedRegistration = _closedRegistration;
                        _closedRegistration = default;
                        _reconnectWaiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                        _reconnectWindowStartTimestamp = _timeProvider.GetTimestamp();
                    }
                    else
                    {
                        terminal = TryTransitionToTerminalLocked(
                            expectedConnection: connection,
                            expectedWaiter: null,
                            exception,
                            out terminalConnection,
                            out terminalClosedRegistration,
                            out terminalClosedRequestedRegistration,
                            out queueAbortCallback);
                    }
                }

                ResetReceivedMessageTimeoutLocked();
            }

            if (reconnecting)
            {
                closedRegistration.Dispose();
                closedRequestedRegistration?.Dispose();

                // Set the current connection to null before waking the old transport so resulting completions are stale.
                connection.Transport.Input.CancelPendingRead();
                connection.Transport.Output.CancelPendingFlush();
            }
            else if (terminal)
            {
                CompleteTerminalTransition(terminalConnection, terminalClosedRegistration, terminalClosedRequestedRegistration, queueAbortCallback);
            }

            return true;
        }

        private bool TimeoutReconnect(TaskCompletionSource<bool> reconnectWaiter)
        {
            ConnectionContext? currentConnection = null;
            CancellationTokenRegistration closedRegistration = default;
            CancellationTokenRegistration? closedRequestedRegistration = null;
            var queueAbortCallback = false;
            var timedOut = false;
            lock (_reconnectLock)
            {
                timedOut = TryTransitionToTerminalLocked(
                    expectedConnection: null,
                    expectedWaiter: reconnectWaiter,
                    exception: null,
                    out currentConnection,
                    out closedRegistration,
                    out closedRequestedRegistration,
                    out queueAbortCallback);
            }

            if (timedOut)
            {
                CompleteTerminalTransition(currentConnection, closedRegistration, closedRequestedRegistration, queueAbortCallback);
                return false;
            }

            return reconnectWaiter.Task.IsCompletedSuccessfully && reconnectWaiter.Task.Result;
        }

        private bool TryTransitionToTerminalLocked(
            ConnectionContext? expectedConnection,
            TaskCompletionSource<bool>? expectedWaiter,
            Exception? exception,
            out ConnectionContext? currentConnection,
            out CancellationTokenRegistration closedRegistration,
            out CancellationTokenRegistration? closedRequestedRegistration,
            out bool queueAbortCallback)
        {
            currentConnection = null;
            closedRegistration = default;
            closedRequestedRegistration = null;
            queueAbortCallback = false;

            if (_connectionAborted ||
                (expectedConnection is not null && !ReferenceEquals(expectedConnection, _currentConnection)) ||
                (expectedWaiter is not null && !ReferenceEquals(expectedWaiter, _reconnectWaiter)))
            {
                return false;
            }

            _connectionAborted = true;
            _reconnectEnabled = false;
            if (exception is not null)
            {
                CloseException = exception;
            }

            currentConnection = _currentConnection;
            _currentConnection = null;
            _detachedConnection = null;
            closedRegistration = _closedRegistration;
            _closedRegistration = default;
            closedRequestedRegistration = _closedRequestedRegistration;
            _closedRequestedRegistration = null;

            var reconnectWaiter = _reconnectWaiter;
            _reconnectWaiter = null;
            _reconnectCandidate = null;
            _reconnectCandidateProtocol = null;
            _reconnectWindowStartTimestamp = null;
            reconnectWaiter?.TrySetResult(false);

            if (!_abortCallbackQueued)
            {
                _abortCallbackQueued = true;
                queueAbortCallback = true;
            }

            return true;
        }

        private void CompleteTerminalTransition(
            ConnectionContext? currentConnection,
            CancellationTokenRegistration closedRegistration,
            CancellationTokenRegistration? closedRequestedRegistration,
            bool queueAbortCallback)
        {
            closedRegistration.Dispose();
            closedRequestedRegistration?.Dispose();

            if (currentConnection is not null)
            {
                // Physical cancellation wakes transport operations but does not cancel the stable connection token.
                currentConnection.Transport.Output.CancelPendingFlush();
                currentConnection.Transport.Input.CancelPendingRead();
            }

            if (queueAbortCallback)
            {
                // We fire and forget since this can trigger user code to run.
                ThreadPool.QueueUserWorkItem(_abortedCallback, this);
            }
        }

        private ConnectionContext? GetCurrentConnection()
        {
            lock (_reconnectLock)
            {
                return _currentConnection;
            }
        }

        private ConnectionContext GetRequiredCurrentConnection() =>
            GetCurrentConnection() ?? throw new InvalidOperationException("No physical transport is currently attached.");

        private TimeSpan GetReconnectWaitTimeoutLocked(TimeSpan requestedTimeout)
        {
            var remainingTimeout = _statefulReconnectTimeout;
            if (_reconnectWindowStartTimestamp is long startTimestamp)
            {
                remainingTimeout -= _timeProvider.GetElapsedTime(startTimestamp);
            }

            if (requestedTimeout != Timeout.InfiniteTimeSpan && requestedTimeout < remainingTimeout)
            {
                remainingTimeout = requestedTimeout;
            }

            return remainingTimeout;
        }

        private bool IsReconnectWindowExpiredLocked() =>
            _reconnectWindowStartTimestamp is long startTimestamp &&
            _timeProvider.GetElapsedTime(startTimestamp) >= _statefulReconnectTimeout;

        private void OnConnectionClosedRequested(ConnectionContext connection)
        {
            ConnectionContext? currentConnection;
            CancellationTokenRegistration closedRegistration;
            CancellationTokenRegistration? closedRequestedRegistration;
            bool queueAbortCallback;
            bool terminal;

            lock (_reconnectLock)
            {
                var isCurrentConnection = ReferenceEquals(connection, _currentConnection);
                var isDetachedConnection = ReferenceEquals(connection, _detachedConnection) &&
                    _reconnectEnabled && _reconnectWaiter is not null && !_reconnectWaiter.Task.IsCompleted;
                if (!isCurrentConnection && !isDetachedConnection)
                {
                    return;
                }

                terminal = TryTransitionToTerminalLocked(
                    expectedConnection: isCurrentConnection ? connection : null,
                    expectedWaiter: null,
                    exception: null,
                    out currentConnection,
                    out closedRegistration,
                    out closedRequestedRegistration,
                    out queueAbortCallback);
            }

            if (terminal)
            {
                CompleteTerminalTransition(currentConnection, closedRegistration, closedRequestedRegistration, queueAbortCallback);
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

        internal void StartClientTimeout()
        {
            if (_clientTimeoutActive)
            {
                return;
            }

            _clientTimeoutActive = true;
            if (GetCurrentConnection() is ConnectionContext connection)
            {
                RegisterClientTimeoutHeartbeat(connection);
            }
        }

        private void CheckClientTimeoutForConnection(ConnectionContext connection)
        {
            if (Debugger.IsAttached || _connectionAborted || !IsCurrentConnection(connection))
            {
                return;
            }

            Exception? timeoutException = null;
            lock (_receiveMessageTimeoutLock)
            {
                if (_receivedMessageTimeoutEnabled)
                {
                    _receivedMessageElapsed = _timeProvider.GetElapsedTime(_receivedMessageTick);

                    if (_receivedMessageElapsed >= _clientTimeoutInterval)
                    {
                        timeoutException = new OperationCanceledException(
                            $"Client hasn't sent a message/ping within the configured {nameof(RaidoConnectionContextOptions.ClientTimeoutInterval)}.");
                    }
                }
            }

            if (timeoutException is not null && TryAbortForConnection(connection, timeoutException))
            {
                Log.ClientTimeout(_logger, _clientTimeoutInterval);
                RaidoEventSource.Log.ConnectionTimedOut(ConnectionId);
            }
        }

        private bool TryAbortForConnection(ConnectionContext connection, Exception exception)
        {
            ConnectionContext? currentConnection;
            CancellationTokenRegistration closedRegistration;
            CancellationTokenRegistration? closedRequestedRegistration;
            bool queueAbortCallback;
            bool terminal;

            lock (_reconnectLock)
            {
                terminal = TryTransitionToTerminalLocked(
                    expectedConnection: connection,
                    expectedWaiter: null,
                    exception,
                    out currentConnection,
                    out closedRegistration,
                    out closedRequestedRegistration,
                    out queueAbortCallback);
            }

            if (terminal)
            {
                CompleteTerminalTransition(currentConnection, closedRegistration, closedRequestedRegistration, queueAbortCallback);
            }

            return terminal;
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
                ResetReceivedMessageTimeoutLocked();
            }
        }

        private void ResetReceivedMessageTimeoutLocked()
        {
            // We received a message or the physical transport detached, so stop and reset the read timer.
            // The replacement transport will start a new timer when its next read begins.
            _receivedMessageElapsed = TimeSpan.Zero;
            _receivedMessageTick = 0;
            _receivedMessageTimeoutEnabled = false;
        }

        private void KeepAliveTick(ConnectionContext connection)
        {
            if (!IsCurrentConnection(connection))
            {
                return;
            }

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
                _ = TryWritePingAsync(connection).Preserve();
            }
        }

        // Don't wait for the lock, if it returns false that means someone wrote to the connection
        // and we don't need to send a ping anymore
        private ValueTask TryWritePingAsync(ConnectionContext connection) =>
            !_writeLock.Wait(0) ? default : new ValueTask(TryWritePingSlowAsyncForConnection(connection));

        private async Task TryWritePingSlowAsyncForConnection(ConnectionContext connection)
        {
            try
            {
                ReadOnlyMemory<byte> pingMessage;
                try
                {
                    if (_connectionAborted || !IsCurrentConnection(connection))
                    {
                        return;
                    }

                    pingMessage = Protocol.GetMessageBytes(PingMessage.Instance);
                }
                catch (Exception ex)
                {
                    Log.FailedWritingMessage(_logger, ex);
                    TryAbortForConnection(connection, ex);
                    return;
                }

                PipeWriter output;
                try
                {
                    output = connection.Transport.Output;
                }
                catch (Exception ex)
                {
                    Log.FailedWritingMessage(_logger, ex);
                    TryAbortForConnection(connection, ex);
                    return;
                }

                try
                {
                    await output.WriteAsync(pingMessage);
                    Log.SentPing(_logger);
                    if (IsCurrentConnection(connection))
                    {
                        // We only update the timestamp after the captured transport successfully sent the ping.
                        Volatile.Write(ref _lastSendTick, _timeProvider.GetTimestamp());
                    }
                }
                catch (OperationCanceledException ex)
                {
                    Log.FailedWritingMessage(_logger, ex);
                    HandleTransportFailure(connection, ex);
                }
                catch (IOException ex)
                {
                    Log.FailedWritingMessage(_logger, ex);
                    HandleTransportFailure(connection, ex);
                }
                catch (ObjectDisposedException ex)
                {
                    Log.FailedWritingMessage(_logger, ex);
                    HandleTransportFailure(connection, ex);
                }
                catch (Exception ex)
                {
                    Log.FailedWritingMessage(_logger, ex);
                    TryAbortForConnection(connection, ex);
                }
            }
            finally
            {
                _writeLock.Release();
            }
        }

        internal void Cleanup()
        {
            var handoffTarget = Interlocked.Exchange(ref _reconnectHandoffTarget, null);
            if (handoffTarget is not null && !_physicalTransportAdopted)
            {
                handoffTarget.FailReconnectCandidate(_connection);
            }

            CancellationTokenRegistration closedRegistration;
            CancellationTokenRegistration? closedRequestedRegistration;
            TaskCompletionSource<bool>? reconnectWaiter;
            lock (_reconnectLock)
            {
                closedRegistration = _closedRegistration;
                _closedRegistration = default;
                closedRequestedRegistration = _closedRequestedRegistration;
                _closedRequestedRegistration = null;
                _currentConnection = null;
                _detachedConnection = null;
                _reconnectCandidate = null;
                _reconnectCandidateProtocol = null;
                _reconnectEnabled = false;
                _reconnectWindowStartTimestamp = null;
                reconnectWaiter = _reconnectWaiter;
                _reconnectWaiter = null;
            }

            closedRegistration.Dispose();
            closedRequestedRegistration?.Dispose();
            reconnectWaiter?.TrySetResult(false);
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

            public static void SentPing(ILogger logger) => _sentPing(logger, null);

            public static void SentMessage(ILogger logger, RaidoMessage message) => _sentMessage(logger, message.GetType().Name, null);

            public static void FailedWritingMessage(ILogger logger, Exception exception) => _failedWritingMessage(logger, exception);

            public static void AbortFailed(ILogger logger, Exception exception) => _abortFailed(logger, exception);

            public static void ClientTimeout(ILogger logger, TimeSpan timeout) => _clientTimeout(logger, (int)timeout.TotalMilliseconds, null);
        }
    }
}
