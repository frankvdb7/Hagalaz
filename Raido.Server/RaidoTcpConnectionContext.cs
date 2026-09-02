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

namespace Raido.Server
{
    /// <summary>
    /// Represents the stable lower Raido connection and its physical transport.
    /// </summary>
    internal sealed class RaidoTcpConnectionContext : ConnectionContext
    {
        private static readonly WaitCallback _abortedCallback = AbortConnection;
        private static readonly TimeSpan MaxSupportedReconnectTimeout =
            TimeSpan.FromMilliseconds(uint.MaxValue - 1L);

        private readonly TaskCompletionSource _abortCompletedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly IFeatureCollection _features;
        private readonly IDictionary<object, object?> _items;
        private readonly CancellationTokenSource _connectionAbortedTokenSource = new();
        private readonly ILogger _logger;
        private readonly Lock _reconnectLock = new();
        private readonly TimeProvider _timeProvider;
        private readonly TimeSpan _statefulReconnectTimeout;

        private CancellationTokenRegistration _closedRegistration;
        private CancellationTokenRegistration? _closedRequestedRegistration;
        private ConnectionContext? _currentPhysicalConnection;
        private ConnectionContext? _detachedPhysicalConnection;
        private TaskCompletionSource<bool>? _reconnectWaiter;
        private long? _reconnectWindowStartTimestamp;

        private volatile bool _connectionAborted;
        private bool _abortCallbackQueued;
        private bool _reconnectEnabled;
        private bool _clientTimeoutHeartbeatEnabled;
        private Action<ConnectionContext>? _keepAliveCallback;
        private Action<ConnectionContext>? _clientTimeoutCallback;

        internal RaidoTcpConnectionContext(ConnectionContext connection, RaidoHubConnectionContextOptions contextOptions, ILoggerFactory loggerFactory)
            : this(connection, contextOptions, loggerFactory, TimeProvider.System)
        {
        }

        internal RaidoTcpConnectionContext(
            ConnectionContext connection,
            RaidoHubConnectionContextOptions contextOptions,
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

            _features = connection.Features;
            _items = connection.Items;
            _logger = loggerFactory.CreateLogger<RaidoTcpConnectionContext>();
            _statefulReconnectTimeout = contextOptions.StatefulReconnectTimeout;
            _reconnectEnabled = contextOptions.StatefulReconnectEnabled;
            _currentPhysicalConnection = connection;
            _connectionId = connection.ConnectionId;
            _timeProvider = timeProvider;

            RegisterPhysicalCallbacks(connection, registerHeartbeat: false, out var closedRegistration, out var closedRequestedRegistration);

            lock (_reconnectLock)
            {
                if (!_connectionAborted && ReferenceEquals(connection, _currentPhysicalConnection))
                {
                    _closedRegistration = closedRegistration;
                    closedRegistration = default;
                    _closedRequestedRegistration = closedRequestedRegistration;
                    closedRequestedRegistration = null;
                }
                else if (!_connectionAborted &&
                    _currentPhysicalConnection is null &&
                    ReferenceEquals(connection, _detachedPhysicalConnection) &&
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

        internal void InitializeHeartbeatCallbacks(
            Action<ConnectionContext> keepAliveCallback,
            Action<ConnectionContext> clientTimeoutCallback)
        {
            _keepAliveCallback = keepAliveCallback;
            _clientTimeoutCallback = clientTimeoutCallback;

            if (GetCurrentPhysicalConnection() is ConnectionContext connection)
            {
                RegisterHeartbeatCallbacks(connection);
            }
        }

        internal void EnableClientTimeoutHeartbeat()
        {
            if (_clientTimeoutHeartbeatEnabled)
            {
                return;
            }

            _clientTimeoutHeartbeatEnabled = true;
            if (GetCurrentPhysicalConnection() is ConnectionContext connection)
            {
                RegisterClientTimeoutHeartbeat(connection);
            }
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
                connection.Features.Get<IConnectionHeartbeatFeature>()?.OnHeartbeat(_ => _keepAliveCallback?.Invoke(connection), this);
            }

            if (_clientTimeoutHeartbeatEnabled)
            {
                RegisterClientTimeoutHeartbeat(connection);
            }
        }

        private void RegisterClientTimeoutHeartbeat(ConnectionContext connection) =>
            connection.Features.Get<IConnectionHeartbeatFeature>()?.OnHeartbeat(
                _ => _clientTimeoutCallback?.Invoke(connection), this);

        internal bool IsCurrentPhysicalConnection(ConnectionContext connection)
        {
            lock (_reconnectLock)
            {
                return ReferenceEquals(connection, _currentPhysicalConnection);
            }
        }

        internal bool TryReconnect(ConnectionContext replacement)
        {
            ArgumentNullException.ThrowIfNull(replacement);

            TaskCompletionSource<bool>? reconnectWaiter;
            ConnectionContext detachedConnection;
            CancellationToken? detachedCloseRequestedToken;
            lock (_reconnectLock)
            {
                if (_connectionAborted || !_reconnectEnabled || _currentPhysicalConnection is not null ||
                    _reconnectWaiter is not TaskCompletionSource<bool> waiter || waiter.Task.IsCompleted ||
                    _detachedPhysicalConnection is not ConnectionContext currentDetachedConnection)
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
                var reconnectWindowIsCurrent = !_connectionAborted && _reconnectEnabled && _currentPhysicalConnection is null &&
                    ReferenceEquals(detachedConnection, _detachedPhysicalConnection) &&
                    ReferenceEquals(reconnectWaiter, _reconnectWaiter) && !reconnectWaiter.Task.IsCompleted;

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
                    _currentPhysicalConnection = replacement;
                    _detachedPhysicalConnection = null;
                    _closedRegistration = closedRegistration;
                    _closedRequestedRegistration = closedRequestedRegistration;
                    _reconnectWaiter = null;
                    _reconnectWindowStartTimestamp = null;
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
                if (_currentPhysicalConnection is null)
                {
                    connection = null!;
                    return false;
                }

                connection = _currentPhysicalConnection;
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

                if (_currentPhysicalConnection is not null)
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

        internal void OnPhysicalConnectionClosed(ConnectionContext connection) => TryDetachPhysicalConnection(connection, exception: null, out _);

        internal bool HandleTransportFailure(ConnectionContext connection, Exception exception)
        {
            var isCurrent = IsCurrentPhysicalConnection(connection);
            var handled = TryDetachPhysicalConnection(connection, exception, out var reconnecting);
            return !isCurrent || reconnecting || !handled;
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

            lock (_reconnectLock)
            {
                if (!ReferenceEquals(connection, _currentPhysicalConnection))
                {
                    reconnecting = false;
                    return false;
                }

                reconnecting = !_connectionAborted && _reconnectEnabled;
                if (reconnecting)
                {
                    _currentPhysicalConnection = null;
                    _detachedPhysicalConnection = connection;
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
                (expectedConnection is not null && !ReferenceEquals(expectedConnection, _currentPhysicalConnection)) ||
                (expectedWaiter is not null && !ReferenceEquals(expectedWaiter, _reconnectWaiter)))
            {
                return false;
            }

            _connectionAborted = true;
            _reconnectEnabled = false;
            currentConnection = _currentPhysicalConnection;
            _currentPhysicalConnection = null;
            _detachedPhysicalConnection = null;
            closedRegistration = _closedRegistration;
            _closedRegistration = default;
            closedRequestedRegistration = _closedRequestedRegistration;
            _closedRequestedRegistration = null;

            var reconnectWaiter = _reconnectWaiter;
            _reconnectWaiter = null;
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

        private ConnectionContext? GetCurrentPhysicalConnection()
        {
            lock (_reconnectLock)
            {
                return _currentPhysicalConnection;
            }
        }

        private ConnectionContext GetRequiredCurrentPhysicalConnection() =>
    GetCurrentPhysicalConnection() ?? throw new InvalidOperationException("No physical transport is currently attached.");

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
                var isCurrentConnection = ReferenceEquals(connection, _currentPhysicalConnection);
                var isDetachedConnection = ReferenceEquals(connection, _detachedPhysicalConnection) &&
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

        internal bool TryAbortForConnection(ConnectionContext connection, Exception exception)
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

        internal Task AbortAsync()
        {
            Abort();
            return _abortCompletedTcs.Task;
        }

        internal void Cleanup()
        {
            CancellationTokenRegistration closedRegistration;
            CancellationTokenRegistration? closedRequestedRegistration;
            TaskCompletionSource<bool>? reconnectWaiter;
            lock (_reconnectLock)
            {
                closedRegistration = _closedRegistration;
                _closedRegistration = default;
                closedRequestedRegistration = _closedRequestedRegistration;
                _closedRequestedRegistration = null;
                _currentPhysicalConnection = null;
                _detachedPhysicalConnection = null;
                _reconnectEnabled = false;
                _reconnectWindowStartTimestamp = null;
                reconnectWaiter = _reconnectWaiter;
                _reconnectWaiter = null;
            }

            closedRegistration.Dispose();
            closedRequestedRegistration?.Dispose();
            reconnectWaiter?.TrySetResult(false);
        }

        private string _connectionId = null!;

        public override string ConnectionId
        {
            get => _connectionId;
            set => _connectionId = value;
        }
        public override IFeatureCollection Features => _features;
        public override IDictionary<object, object?> Items
        {
            get => _items;
            set => throw new NotSupportedException("The stable connection owns its Items collection.");
        }
        public override IDuplexPipe Transport
        {
            get => GetRequiredCurrentPhysicalConnection().Transport;
            set => throw new NotSupportedException("The stable connection owns its physical transport.");
        }
        public override CancellationToken ConnectionClosed => _connectionAbortedTokenSource.Token;
        public override IPEndPoint? LocalEndPoint => GetCurrentPhysicalConnection()?.LocalEndPoint as IPEndPoint;
        public override IPEndPoint? RemoteEndPoint => GetCurrentPhysicalConnection()?.RemoteEndPoint as IPEndPoint;

        public override void Abort()
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

        private static void AbortConnection(object? state)
        {
            var connection = (RaidoTcpConnectionContext)state!;

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
                connection._abortCompletedTcs.TrySetResult();
            }
        }

        private static class Log
        {
            private static readonly Action<ILogger, Exception> _abortFailed =
                LoggerMessage.Define(LogLevel.Trace, new EventId(4, "AbortFailed"), "Abort callback failed.");

            public static void AbortFailed(ILogger logger, Exception exception) => _abortFailed(logger, exception);
        }
    }
}