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
    /// Represents one logical connection to a Raido Hub.
    /// </summary>
    public class RaidoHubConnectionContext
    {
        private readonly ConnectionContext _connectionContext;
        private readonly ILogger _logger;
        private readonly Lock _receiveMessageTimeoutLock = new();
        private readonly TimeProvider _timeProvider;
        private readonly SemaphoreSlim _writeLock = new(1);
        private readonly TimeSpan _keepAliveInterval;
        private readonly TimeSpan _clientTimeoutInterval;

        private ClaimsPrincipal? _user;
        private volatile bool _clientTimeoutActive;
        private long _lastSendTick;
        private TimeSpan _receivedMessageElapsed;
        private bool _receivedMessageTimeoutEnabled;
        private long _receivedMessageTick;

        internal long StartTimestamp { get; set; }
        internal RaidoCallerContext RaidoCallerContext { get; }
        internal IRaidoCallerClients RaidoCallerClients { get; set; } = null!;
        internal Activity? OriginalActivity { get; set; }
        internal MetricsContext MetricsContext { get; set; }

        internal RaidoTcpConnectionContext TcpConnection => (RaidoTcpConnectionContext)_connectionContext;

        public virtual CancellationToken ConnectionAbortedToken => _connectionContext.ConnectionClosed;
        public virtual string ConnectionId => _connectionContext.ConnectionId;

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

        public virtual IFeatureCollection Features => _connectionContext.Features;
        public virtual IDictionary<object, object?> Items => _connectionContext.Items;
        public virtual IPEndPoint? LocalEndPoint => _connectionContext.LocalEndPoint as IPEndPoint;
        public virtual IPEndPoint? RemoteEndPoint => _connectionContext.RemoteEndPoint as IPEndPoint;
        public virtual IRaidoProtocol Protocol { get; internal set; } = default!;

        public RaidoHubConnectionContext(ConnectionContext connection, RaidoConnectionContextOptions contextOptions, ILoggerFactory loggerFactory)
            : this(CreateTcpConnection(connection, contextOptions, loggerFactory), contextOptions, loggerFactory, TimeProvider.System)
        {
        }

        private static RaidoTcpConnectionContext CreateTcpConnection(
            ConnectionContext connection,
            RaidoConnectionContextOptions contextOptions,
            ILoggerFactory loggerFactory)
        {
            var tcpConnection = new RaidoTcpConnectionContext(contextOptions, loggerFactory);
            if (!tcpConnection.TryActivatePersistentConnection(connection))
            {
                throw new InvalidOperationException("The initial physical connection could not be activated.");
            }

            return tcpConnection;
        }

        internal RaidoHubConnectionContext(
            RaidoTcpConnectionContext connection,
            RaidoConnectionContextOptions contextOptions,
            ILoggerFactory loggerFactory,
            TimeProvider timeProvider)
        {
            _connectionContext = connection;
            _logger = loggerFactory.CreateLogger<RaidoHubConnectionContext>();
            _clientTimeoutInterval = contextOptions.ClientTimeoutInterval;
            _keepAliveInterval = contextOptions.KeepAliveInterval;
            _timeProvider = timeProvider;
            _lastSendTick = _timeProvider.GetTimestamp();
            RaidoCallerContext = new DefaultRaidoCallerContext(this);
        }

        internal Task OnConnectedAsync()
        {
            Features.Get<IConnectionHeartbeatFeature>()?.OnHeartbeat(
                static state => ((RaidoHubConnectionContext)state!).KeepAliveTick(),
                this);
            StartTimestamp = _timeProvider.GetTimestamp();
            return Task.CompletedTask;
        }

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

            if (ConnectionAbortedToken.IsCancellationRequested && !ignoreAbort)
            {
                _writeLock.Release();
                return default;
            }

            // This method should never throw synchronously
            var task = WriteCore(_connectionContext, message, cancellationToken);

            // The write didn't complete synchronously so await completion
            if (!task.IsCompletedSuccessfully)
            {
                return new ValueTask(CompleteWriteAndReleaseAsync(_connectionContext, task, cancellationToken));
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
            PipeWriter output;
            try
            {
                // We know that we are only writing this message to one receiver, so we can
                // write it without caching.
                if (!TcpConnection.TryWriteStableTransport(
                        target => Protocol.WriteMessage(message, target),
                        out output))
                {
                    return new ValueTask<FlushResult>(new FlushResult(isCanceled: false, isCompleted: false));
                }
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                TcpConnection.AbortWithException(ex);
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
                TcpConnection.AbortWithException(ex);
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
                TcpConnection.AbortWithException(ex);
            }
            catch (IOException ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                TcpConnection.AbortWithException(ex);
            }
            catch (ObjectDisposedException ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                TcpConnection.AbortWithException(ex);
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                TcpConnection.AbortWithException(ex);
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
                TcpConnection.AbortWithException(ex);
            }
            catch (IOException ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                TcpConnection.AbortWithException(ex);
            }
            catch (ObjectDisposedException ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                TcpConnection.AbortWithException(ex);
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                TcpConnection.AbortWithException(ex);
            }
        }

        private async Task WriteSlowAsync<TMessage>(TMessage message, bool ignoreAbort, CancellationToken cancellationToken) where TMessage : RaidoMessage
        {
            // Failed to get the lock immediately when entering WriteAsync so await until it is available
            await _writeLock.WaitAsync(cancellationToken);
            try
            {
                if (ConnectionAbortedToken.IsCancellationRequested && !ignoreAbort)
                {
                    return;
                }

                await CompleteWriteAsync(_connectionContext, WriteCore(_connectionContext, message, cancellationToken), cancellationToken);
            }
            finally
            {
                _writeLock.Release();
            }
        }



        public virtual void Abort() => TcpConnection.Abort();

        internal async Task AbortAsync()
        {
            var abortTask = TcpConnection.AbortAsync();
            await _writeLock.WaitAsync().ConfigureAwait(false);
            _writeLock.Release();
            await abortTask.ConfigureAwait(false);
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

        private void ResetReceivedMessageTimeout()
        {
            lock (_receiveMessageTimeoutLock)
            {
                ResetReceivedMessageTimeoutLocked();
            }
        }

        private void ResetReceivedMessageTimeoutLocked()
        {
            _receivedMessageElapsed = TimeSpan.Zero;
            _receivedMessageTick = 0;
            _receivedMessageTimeoutEnabled = false;
        }

        internal void StartClientTimeout()
        {
            if (_clientTimeoutActive)
            {
                return;
            }

            _clientTimeoutActive = true;
            Features.Get<IConnectionHeartbeatFeature>()?.OnHeartbeat(
                static state => ((RaidoHubConnectionContext)state!).CheckClientTimeout(),
                this);
        }

        private void CheckClientTimeout()
        {
            if (Debugger.IsAttached || !TcpConnection.IsActive)
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

            if (timeoutException is not null && TcpConnection.TryAbortIfActive(timeoutException))
            {
                Log.ClientTimeout(_logger, _clientTimeoutInterval);
                RaidoEventSource.Log.ConnectionTimedOut(ConnectionId);
            }
        }

        private void KeepAliveTick()
        {
            if (!TcpConnection.IsActive)
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
                _ = TryWritePingAsync().Preserve();
            }
        }

        // Don't wait for the lock, if it returns false that means someone wrote to the connection
        // and we don't need to send a ping anymore
        private ValueTask TryWritePingAsync() =>
            !_writeLock.Wait(0) ? default : new ValueTask(TryWritePingSlowAsync());

        private async Task TryWritePingSlowAsync()
        {
            try
            {
                ReadOnlyMemory<byte> pingMessage;
                try
                {
                    if (!TcpConnection.IsActive)
                    {
                        return;
                    }

                    pingMessage = Protocol.GetMessageBytes(PingMessage.Instance);
                }
                catch (Exception ex)
                {
                    Log.FailedWritingMessage(_logger, ex);
                    TcpConnection.AbortWithException(ex);
                    return;
                }

                try
                {
                    if (!TcpConnection.TryWriteStableTransport(
                            output =>
                            {
                                var destination = output.GetSpan(pingMessage.Length);
                                pingMessage.Span.CopyTo(destination);
                                output.Advance(pingMessage.Length);
                            },
                            out var output))
                    {
                        return;
                    }

                    await output.FlushAsync();
                    if (TcpConnection.IsActive)
                    {
                        Log.SentPing(_logger);
                        // We only update the timestamp after the captured transport successfully sent the ping.
                        Volatile.Write(ref _lastSendTick, _timeProvider.GetTimestamp());
                    }
                }
                catch (OperationCanceledException ex)
                {
                    Log.FailedWritingMessage(_logger, ex);
                    TcpConnection.AbortWithException(ex);
                }
                catch (IOException ex)
                {
                    Log.FailedWritingMessage(_logger, ex);
                    TcpConnection.AbortWithException(ex);
                }
                catch (ObjectDisposedException ex)
                {
                    Log.FailedWritingMessage(_logger, ex);
                    TcpConnection.AbortWithException(ex);
                }
                catch (Exception ex)
                {
                    Log.FailedWritingMessage(_logger, ex);
                    TcpConnection.AbortWithException(ex);
                }
            }
            finally
            {
                _writeLock.Release();
            }
        }



        internal Task CleanupAsync() => TcpConnection.CleanupAsync();

        private static class Log
        {
            private static readonly Action<ILogger, Exception?> _sentPing =
                LoggerMessage.Define(LogLevel.Trace, new EventId(1, "SentPing"), "Sent a ping message to the client.");

            private static readonly Action<ILogger, string, Exception?> _sentMessage =
                LoggerMessage.Define<string>(LogLevel.Trace, new EventId(2, "SentMessage"), "Sent a {Message} to the client.");

            private static readonly Action<ILogger, Exception> _failedWritingMessage = LoggerMessage.Define(LogLevel.Debug,
                new EventId(3, "FailedWritingMessage"),
                "Failed writing message. Aborting connection.");

            private static readonly Action<ILogger, int, Exception?> _clientTimeout = LoggerMessage.Define<int>(LogLevel.Debug,
                new EventId(5, "ClientTimeout"),
                "Client timeout ({ClientTimeout}ms) elapsed without receiving a message from the client. Closing connection.");

            public static void SentPing(ILogger logger) => _sentPing(logger, null);
            public static void SentMessage(ILogger logger, RaidoMessage message) => _sentMessage(logger, message.GetType().Name, null);
            public static void FailedWritingMessage(ILogger logger, Exception exception) => _failedWritingMessage(logger, exception);
            public static void ClientTimeout(ILogger logger, TimeSpan timeout) => _clientTimeout(logger, (int)timeout.TotalMilliseconds, null);
        }
    }
}
