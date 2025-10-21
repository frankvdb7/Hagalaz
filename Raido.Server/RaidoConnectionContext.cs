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
        private readonly CancellationTokenRegistration _closedRegistration;
        private readonly CancellationTokenRegistration? _closedRequestedRegistration;

        private readonly ILogger _logger;
        private readonly Lock _receiveMessageTimeoutLock = new();
        private readonly TimeProvider _timeProvider;
        private readonly SemaphoreSlim _writeLock = new(1);
        private ClaimsPrincipal? _user;

        private bool _clientTimeoutActive;
        private volatile bool _connectionAborted;
        private long _lastSendTick;
        private TimeSpan _receivedMessageElapsed;
        private bool _receivedMessageTimeoutEnabled;
        private long _receivedMessageTick;
        private IPEndPoint? _localIPEndPoint;
        private IPEndPoint? _remoteIPEndPoint;

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
        public virtual PipeReader Input => _connection.Transport.Input;

        /// <summary>
        /// Gets the output pipe for the connection.
        /// </summary>
        public virtual PipeWriter Output => _connection.Transport.Output;

        /// <summary>
        /// Gets the local endpoint for the connection.
        /// </summary>
        public virtual IPEndPoint? LocalEndPoint => _localIPEndPoint ??= _connection.LocalEndPoint as IPEndPoint;

        /// <summary>
        /// Gets the remote endpoint for the connection.
        /// </summary>
        public virtual IPEndPoint? RemoteEndPoint => _remoteIPEndPoint ??= _connection.RemoteEndPoint as IPEndPoint;


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
            ConnectionAbortedToken = _connectionAbortedTokenSource.Token;
            _closedRegistration = connection.ConnectionClosed.Register((state) => ((RaidoConnectionContext)state!).Abort(), this);
            if (connection.Features.Get<IConnectionLifetimeNotificationFeature>() is IConnectionLifetimeNotificationFeature lifetimeNotification)
            {
                // This feature is used by HttpConnectionManager to close the connection with a non-errored closed message on authentication expiration.
                _closedRequestedRegistration =
                    lifetimeNotification.ConnectionClosedRequested.Register(static (state) => ((RaidoConnectionContext)state!).Abort(), this);
            }

            RaidoCallerContext = new DefaultRaidoCallerContext(this);

            _timeProvider = TimeProvider.System;
            _lastSendTick = _timeProvider.GetTimestamp();
        }

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

            if (_connectionAborted && !ignoreAbort)
            {
                _writeLock.Release();
                return default;
            }

            // This method should never throw synchronously
            var task = WriteCore(message, cancellationToken);

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

        private ValueTask<FlushResult> WriteCore<TMessage>(TMessage message, CancellationToken cancellationToken) where TMessage : RaidoMessage
        {
            try
            {
                // We know that we are only writing this message to one receiver, so we can
                // write it without caching.
                Protocol.WriteMessage(message, _connection.Transport.Output);

                // check if there is actually a message encoded
                if (!_connection.Transport.Output.CanGetUnflushedBytes || _connection.Transport.Output.UnflushedBytes > 0)
                {
                    Log.SentMessage(_logger, message);
                }

                return _connection.Transport.Output.FlushAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                CloseException = ex;
                Log.FailedWritingMessage(_logger, ex);

                Abort();

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

                Abort();
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

            try
            {
                if (_connectionAborted && !ignoreAbort)
                {
                    return;
                }

                await WriteCore(message, cancellationToken);
            }
            catch (Exception ex)
            {
                CloseException = ex;
                Log.FailedWritingMessage(_logger, ex);
                Abort();
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
            _connectionAborted = true;

            // Cancel any current writes or writes that are about to happen and have already gone past the _connectionAborted bool
            // We have to do this outside of the lock otherwise it could hang if the write is observing backpressure
            _connection.Transport.Output.CancelPendingFlush();

            // If we already triggered the token then noop, this isn't thread safe but it's good enough
            // to avoid spawning a new task in the most common cases
            if (_connectionAbortedTokenSource.IsCancellationRequested)
            {
                return;
            }

            Input.CancelPendingRead();

            // We fire and forget since this can trigger user code to run
            ThreadPool.QueueUserWorkItem(_abortedCallback, this);
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
            Features.Get<IConnectionHeartbeatFeature>()?.OnHeartbeat(state => ((RaidoConnectionContext)state).CheckClientTimeout(), this);
        }

        private void CheckClientTimeout()
        {
            if (Debugger.IsAttached || _connectionAborted)
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
                if (_connectionAborted)
                {
                    return;
                }

                var pingMessage = Protocol.GetMessageBytes(PingMessage.Instance);
                await _connection.Transport.Output.WriteAsync(pingMessage);

                Log.SentPing(_logger);
            }
            catch (Exception ex)
            {
                CloseException = ex;
                Log.FailedWritingMessage(_logger, ex);
                Abort();
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