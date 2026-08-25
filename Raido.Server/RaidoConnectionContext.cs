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
        private TaskCompletionSource<bool>? _reconnectWaiter;
        private ClaimsPrincipal? _user;

        private volatile bool _clientTimeoutActive;
        private volatile bool _connectionAborted;
        private bool _reconnectEnabled;
        private long _lastSendTick;
        private TimeSpan _receivedMessageElapsed;
        private bool _receivedMessageTimeoutEnabled;
        private long _receivedMessageTick;

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
        {
            _connection = connection;
            _logger = loggerFactory.CreateLogger<RaidoConnectionContext>();

            _clientTimeoutInterval = contextOptions.ClientTimeoutInterval;
            _keepAliveInterval = contextOptions.KeepAliveInterval;
            _statefulReconnectTimeout = contextOptions.StatefulReconnectTimeout;
            _reconnectEnabled = contextOptions.StatefulReconnectEnabled;
            _currentConnection = connection;
            ConnectionAbortedToken = _connectionAbortedTokenSource.Token;

            RaidoCallerContext = new DefaultRaidoCallerContext(this);

            _timeProvider = TimeProvider.System;
            _lastSendTick = _timeProvider.GetTimestamp();

            RegisterPhysicalCallbacks(connection, registerHeartbeat: false, out _closedRegistration, out _closedRequestedRegistration);
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
            closedRegistration = connection.ConnectionClosed.Register(() => OnPhysicalConnectionClosed(connection));
            closedRequestedRegistration = null;
            try
            {
                if (connection.Features.Get<IConnectionLifetimeNotificationFeature>() is IConnectionLifetimeNotificationFeature lifetimeNotification)
                {
                    // This feature is used by HttpConnectionManager to close a connection with a non-errored closed message.
                    closedRequestedRegistration = lifetimeNotification.ConnectionClosedRequested
                        .Register(() => OnPhysicalConnectionClosed(connection));
                }
                else
                {
                    closedRequestedRegistration = null;
                }

                if (registerHeartbeat)
                {
                    RegisterHeartbeatCallbacks(connection);
                }
            }
            catch
            {
                closedRegistration.Dispose();
                closedRequestedRegistration?.Dispose();
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
                return new ValueTask(CompleteWriteAsync(currentConnection, task));
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

        private ValueTask<FlushResult> WriteCore<TMessage>(ConnectionContext connection, TMessage message, CancellationToken cancellationToken)
            where TMessage : RaidoMessage
        {
            try
            {
                // We know that we are only writing this message to one receiver, so we can
                // write it without caching.
                var output = connection.Transport.Output;
                Protocol.WriteMessage(message, output);

                // check if there is actually a message encoded
                if (!output.CanGetUnflushedBytes || output.UnflushedBytes > 0)
                {
                    Log.SentMessage(_logger, message);
                }

                return output.FlushAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                HandleTransportFailure(connection, ex);

                return new ValueTask<FlushResult>(new FlushResult(isCanceled: false, isCompleted: true));
            }
        }

        private async Task CompleteWriteAsync(ConnectionContext connection, ValueTask<FlushResult> task)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                HandleTransportFailure(connection, ex);
            }
            finally
            {
                // Release the lock acquired when entering WriteAsync
                _writeLock.Release();
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

                await WriteCore(currentConnection, message, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                if (currentConnection is not null)
                {
                    HandleTransportFailure(currentConnection, ex);
                }
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

            lock (_reconnectLock)
            {
                _reconnectEnabled = false;

                if (_reconnectWaiter is TaskCompletionSource<bool> waiter)
                {
                    _reconnectWaiter = null;
                    waiter.TrySetResult(false);
                }

                if (!_connectionAborted)
                {
                    _connectionAborted = true;
                    currentConnection = _currentConnection;
                    _currentConnection = null;
                    closedRegistration = _closedRegistration;
                    _closedRegistration = default;
                    closedRequestedRegistration = _closedRequestedRegistration;
                    _closedRequestedRegistration = null;
                }
            }

            closedRegistration.Dispose();
            closedRequestedRegistration?.Dispose();

            if (currentConnection is not null)
            {
                // Physical cancellation wakes transport operations but does not cancel the stable connection token.
                currentConnection.Transport.Output.CancelPendingFlush();
                currentConnection.Transport.Input.CancelPendingRead();
            }

            // If we already triggered the token then noop, this isn't thread safe but it's good enough
            // to avoid spawning a new task in the most common cases
            if (_connectionAbortedTokenSource.IsCancellationRequested)
            {
                return;
            }

            // We fire and forget since this can trigger user code to run
            ThreadPool.QueueUserWorkItem(_abortedCallback, this);
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
            lock (_reconnectLock)
            {
                if (_connectionAborted || !_reconnectEnabled || _currentConnection is not null ||
                    _reconnectWaiter is not TaskCompletionSource<bool> waiter || waiter.Task.IsCompleted)
                {
                    return false;
                }

                reconnectWaiter = waiter;
            }

            CancellationTokenRegistration closedRegistration = default;
            CancellationTokenRegistration? closedRequestedRegistration = null;
            try
            {
                RegisterPhysicalCallbacks(replacement, registerHeartbeat: true, out closedRegistration, out closedRequestedRegistration);
            }
            catch
            {
                closedRegistration.Dispose();
                closedRequestedRegistration?.Dispose();
                return false;
            }

            var closedRequestedToken = replacement.Features.Get<IConnectionLifetimeNotificationFeature>()?.ConnectionClosedRequested;
            var published = false;

            lock (_reconnectLock)
            {
                if (!_connectionAborted && _reconnectEnabled && _currentConnection is null &&
                    ReferenceEquals(reconnectWaiter, _reconnectWaiter) && !reconnectWaiter.Task.IsCompleted &&
                    !replacement.ConnectionClosed.IsCancellationRequested &&
                    !closedRequestedToken.GetValueOrDefault().IsCancellationRequested)
                {
                    _currentConnection = replacement;
                    _closedRegistration = closedRegistration;
                    _closedRequestedRegistration = closedRequestedRegistration;
                    _reconnectWaiter = null;
                    reconnectWaiter.TrySetResult(true);
                    published = true;
                }
            }

            if (!published)
            {
                closedRegistration.Dispose();
                closedRequestedRegistration?.Dispose();
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
            }

            if (timeout == Timeout.InfiniteTimeSpan)
            {
                return await reconnectWaiter.Task.ConfigureAwait(false);
            }

            if (timeout <= TimeSpan.Zero)
            {
                return TimeoutReconnect(reconnectWaiter);
            }

            try
            {
                return await reconnectWaiter.Task.WaitAsync(timeout).ConfigureAwait(false);
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
            var abort = false;

            lock (_reconnectLock)
            {
                if (!ReferenceEquals(connection, _currentConnection))
                {
                    reconnecting = false;
                    return false;
                }

                if (exception is not null)
                {
                    CloseException = exception;
                }

                _currentConnection = null;
                closedRegistration = _closedRegistration;
                _closedRegistration = default;
                closedRequestedRegistration = _closedRequestedRegistration;
                _closedRequestedRegistration = null;

                reconnecting = !_connectionAborted && _reconnectEnabled;
                if (reconnecting)
                {
                    _reconnectWaiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                }
                else
                {
                    _reconnectEnabled = false;
                    _connectionAborted = true;
                    _reconnectWaiter = null;
                    abort = true;
                }
            }

            closedRegistration.Dispose();
            closedRequestedRegistration?.Dispose();

            // Set the current connection to null before waking the old transport so resulting completions are stale.
            connection.Transport.Input.CancelPendingRead();
            connection.Transport.Output.CancelPendingFlush();

            if (abort)
            {
                Abort();
            }

            return true;
        }

        private bool TimeoutReconnect(TaskCompletionSource<bool> reconnectWaiter)
        {
            var timedOut = false;
            lock (_reconnectLock)
            {
                if (ReferenceEquals(reconnectWaiter, _reconnectWaiter) && !reconnectWaiter.Task.IsCompleted && !_connectionAborted)
                {
                    _reconnectEnabled = false;
                    _connectionAborted = true;
                    _reconnectWaiter = null;
                    reconnectWaiter.TrySetResult(false);
                    timedOut = true;
                }
            }

            if (timedOut)
            {
                Abort();
                return false;
            }

            return reconnectWaiter.Task.IsCompletedSuccessfully && reconnectWaiter.Task.Result;
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

        private void CheckClientTimeout()
        {
            if (GetCurrentConnection() is ConnectionContext connection)
            {
                CheckClientTimeoutForConnection(connection);
            }
        }

        private void CheckClientTimeoutForConnection(ConnectionContext connection)
        {
            if (Debugger.IsAttached || _connectionAborted || !IsCurrentConnection(connection))
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
                        Abort();
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

                // We only update the timestamp here, because updating on each sent message is bad for performance
                // There can be a lot of sent messages per 15 seconds
                Volatile.Write(ref _lastSendTick, currentTime);
            }
        }

        // Don't wait for the lock, if it returns false that means someone wrote to the connection
        // and we don't need to send a ping anymore
        private ValueTask TryWritePingAsync(ConnectionContext connection) =>
            !_writeLock.Wait(0) ? default : new ValueTask(TryWritePingSlowAsyncForConnection(connection));

        private Task TryWritePingSlowAsync()
        {
            var connection = GetCurrentConnection();
            return connection is null ? Task.CompletedTask : TryWritePingSlowAsyncForConnection(connection);
        }

        private async Task TryWritePingSlowAsyncForConnection(ConnectionContext connection)
        {
            try
            {
                if (_connectionAborted || !IsCurrentConnection(connection))
                {
                    return;
                }

                var pingMessage = Protocol.GetMessageBytes(PingMessage.Instance);
                await connection.Transport.Output.WriteAsync(pingMessage);

                Log.SentPing(_logger);
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                HandleTransportFailure(connection, ex);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        internal void Cleanup()
        {
            CancellationTokenRegistration closedRegistration;
            CancellationTokenRegistration? closedRequestedRegistration;
            lock (_reconnectLock)
            {
                closedRegistration = _closedRegistration;
                _closedRegistration = default;
                closedRequestedRegistration = _closedRequestedRegistration;
                _closedRequestedRegistration = null;
                _currentConnection = null;
                _reconnectEnabled = false;
            }

            closedRegistration.Dispose();
            closedRequestedRegistration?.Dispose();
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
