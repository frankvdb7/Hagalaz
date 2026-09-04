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
        private readonly RaidoTcpConnectionContext _tcpConnection;
        private readonly ILogger _logger;
        private readonly Lock _receiveMessageTimeoutLock = new();
        private readonly TimeProvider _timeProvider;
        private readonly SemaphoreSlim _writeLock = new(1);
        private readonly TimeSpan _keepAliveInterval;
        private readonly TimeSpan _clientTimeoutInterval;
        private IRaidoProtocol _protocol;
        private IAsyncDisposable? _protocolLifetime;

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

        public CancellationToken ConnectionAborted => _connectionContext.ConnectionClosed;
        public string ConnectionId => _connectionContext.ConnectionId;

        public ClaimsPrincipal? User
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

        public IFeatureCollection Features => _connectionContext.Features;
        public IDictionary<object, object?> Items => _connectionContext.Items;
        public IPEndPoint? LocalEndPoint => _connectionContext.LocalEndPoint as IPEndPoint;
        public IPEndPoint? RemoteEndPoint => _connectionContext.RemoteEndPoint as IPEndPoint;

        /// <summary>
        /// Gets the protocol used for the next protocol read and for writes that begin after a transition completes.
        /// </summary>
        public IRaidoProtocol Protocol => Volatile.Read(ref _protocol);

        internal PipeReader TransportInput => _tcpConnection.Transport.Input;
        internal bool IsTerminal => _tcpConnection.IsTerminal;
        internal bool IsReconnectEnabled => _tcpConnection.IsReconnectEnabled;
        internal Exception? TerminalException => _tcpConnection.TerminalException;

        internal RaidoHubConnectionContext(
            RaidoTcpConnectionContext connection,
            RaidoConnectionContextOptions contextOptions,
            IRaidoProtocol protocol,
            ILoggerFactory loggerFactory,
            TimeProvider timeProvider)
        {
            _connectionContext = connection;
            _tcpConnection = connection;
            _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            _logger = loggerFactory.CreateLogger<RaidoHubConnectionContext>();
            _clientTimeoutInterval = contextOptions.ClientTimeoutInterval;
            _keepAliveInterval = contextOptions.KeepAliveInterval;
            _timeProvider = timeProvider;
            _lastSendTick = _timeProvider.GetTimestamp();
            RaidoCallerContext = new DefaultRaidoCallerContext(this);
        }

        /// <summary>
        /// Replaces the protocol after writes using the current protocol have completed.
        /// </summary>
        /// <remarks>
        /// The existing write lock serializes this transition with Raido writes. A write already in progress with the
        /// previous protocol completes before this operation changes <see cref="Protocol"/>; writes that begin after
        /// the transition use the new protocol. A read already started with the previous protocol is not
        /// reinterpreted by this operation. Once the new protocol is installed after acquiring the write boundary, the
        /// transition is committed; failure while disposing a previous owned lifetime is reported as a cleanup failure
        /// and does not roll back the protocol. Normal Hub dispatch ordering completes a transition performed by a Hub
        /// method before the next message read begins.
        /// </remarks>
        /// <param name="protocol">The protocol to use for subsequent reads and writes.</param>
        /// <param name="cancellationToken">The token that cancels waiting for the write boundary.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the transition.</returns>
        public async ValueTask SetProtocolAsync(
            IRaidoProtocol protocol,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(protocol);

            await SetProtocolCoreAsync(protocol, protocolLifetime: null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Replaces the protocol after writes using the current protocol have completed and transfers ownership of the supplied lifetime to this connection.
        /// </summary>
        /// <remarks>
        /// The lifetime is disposed when this protocol is replaced or when the logical connection is cleaned up. The
        /// lifetime is also disposed if cancellation prevents the transition from acquiring the write boundary or if
        /// the logical connection is terminal when the transition acquires the write boundary. The transition is
        /// rejected without changing <see cref="Protocol"/>, and the incoming lifetime is disposed. The transition
        /// has the same write and read guarantees as <see cref="SetProtocolAsync(IRaidoProtocol, CancellationToken)"/>.
        /// </remarks>
        /// <param name="protocol">The protocol to use for subsequent reads and writes.</param>
        /// <param name="protocolLifetime">The lifetime for the protocol and its connection-owned dependencies.</param>
        /// <param name="cancellationToken">The token that cancels waiting for the write boundary.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the transition.</returns>
        public ValueTask SetProtocolAsync(
            IRaidoProtocol protocol,
            IAsyncDisposable protocolLifetime,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(protocol);
            ArgumentNullException.ThrowIfNull(protocolLifetime);

            return SetProtocolCoreAsync(protocol, protocolLifetime, cancellationToken);
        }

        private async ValueTask SetProtocolCoreAsync(
            IRaidoProtocol protocol,
            IAsyncDisposable? protocolLifetime,
            CancellationToken cancellationToken)
        {
            try
            {
                await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                if (protocolLifetime is not null)
                {
                    await protocolLifetime.DisposeAsync().ConfigureAwait(false);
                }

                throw;
            }

            try
            {
                if (_tcpConnection.IsTerminal)
                {
                    if (protocolLifetime is not null)
                    {
                        await protocolLifetime.DisposeAsync().ConfigureAwait(false);
                    }

                    throw new ObjectDisposedException(nameof(RaidoHubConnectionContext));
                }

                var previousProtocolLifetime = _protocolLifetime;
                _protocolLifetime = protocolLifetime;
                Volatile.Write(ref _protocol, protocol);

                if (previousProtocolLifetime is not null)
                {
                    await previousProtocolLifetime.DisposeAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                _writeLock.Release();
            }
        }

        internal Task<bool> WaitForReconnectAsync() => _tcpConnection.WaitForReconnectAsync();

        internal void AcknowledgeInputBoundary() => _tcpConnection.AcknowledgeInputBoundary();

        internal void CompleteTransportInput() => _tcpConnection.CompleteTransportInput();

        internal bool TryAttachPhysicalConnection(ConnectionContext connection) => _tcpConnection.TryAttachPhysicalConnection(connection);

        /// <summary>
        /// Attempts to attach a raw replacement transport to this existing logical connection.
        /// </summary>
        /// <param name="replacement">The replacement physical connection.</param>
        /// <returns><see langword="true"/> when the existing reconnect window accepts the transport.</returns>
        public bool TryReconnect(ConnectionContext replacement) => _tcpConnection.TryAttachPhysicalConnection(replacement);

        internal Task OnConnectedAsync()
        {
            Features.Get<IConnectionHeartbeatFeature>()?.OnHeartbeat(
                static state => ((RaidoHubConnectionContext)state!).KeepAliveTick(),
                this);
            StartTimestamp = _timeProvider.GetTimestamp();
            return Task.CompletedTask;
        }

        public ValueTask WriteAsync<TMessage>(TMessage message) where TMessage : RaidoMessage =>
            WriteAsync(message, CancellationToken.None);

        public ValueTask WriteAsync<TMessage>(TMessage message, CancellationToken cancellationToken) where TMessage : RaidoMessage =>
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

            if (ConnectionAborted.IsCancellationRequested && !ignoreAbort)
            {
                _writeLock.Release();
                return default;
            }

            // This method should never throw synchronously
            var task = WriteCore(message, cancellationToken);

            // The write didn't complete synchronously so await completion
            if (!task.IsCompletedSuccessfully)
            {
                return new ValueTask(CompleteWriteAndReleaseAsync(task, cancellationToken));
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

        private ValueTask<FlushResult> WriteCore<TMessage>(TMessage message, CancellationToken cancellationToken)
            where TMessage : RaidoMessage
        {
            bool hasWrittenBytes;
            ValueTask<FlushResult> flushTask;
            try
            {
                // We know that we are only writing this message to one receiver, so we can
                // write it without caching.
                if (!_tcpConnection.TryWriteStableTransport(
                        target => Protocol.WriteMessage(message, target),
                        cancellationToken,
                        out hasWrittenBytes,
                        out flushTask))
                {
                    return new ValueTask<FlushResult>(new FlushResult(isCanceled: false, isCompleted: false));
                }
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                _tcpConnection.AbortWithException(ex);
                return new ValueTask<FlushResult>(new FlushResult(isCanceled: false, isCompleted: true));
            }

            try
            {
                if (hasWrittenBytes)
                {
                    Log.SentMessage(_logger, message);
                }
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                _tcpConnection.AbortWithException(ex);
                return new ValueTask<FlushResult>(new FlushResult(isCanceled: false, isCompleted: true));
            }

            try
            {
                return flushTask;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return new ValueTask<FlushResult>(Task.FromCanceled<FlushResult>(cancellationToken));
            }
            catch (OperationCanceledException ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                _tcpConnection.AbortWithException(ex);
            }
            catch (IOException ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                _tcpConnection.AbortWithException(ex);
            }
            catch (ObjectDisposedException ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                _tcpConnection.AbortWithException(ex);
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                _tcpConnection.AbortWithException(ex);
            }

            return new ValueTask<FlushResult>(new FlushResult(isCanceled: false, isCompleted: true));
        }

        private async Task CompleteWriteAndReleaseAsync(ValueTask<FlushResult> task, CancellationToken cancellationToken)
        {
            try
            {
                await CompleteWriteAsync(task, cancellationToken);
            }
            finally
            {
                // Release the lock acquired when entering WriteAsync
                _writeLock.Release();
            }
        }

        private async Task CompleteWriteAsync(ValueTask<FlushResult> task, CancellationToken cancellationToken)
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
                _tcpConnection.AbortWithException(ex);
            }
            catch (IOException ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                _tcpConnection.AbortWithException(ex);
            }
            catch (ObjectDisposedException ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                _tcpConnection.AbortWithException(ex);
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                _tcpConnection.AbortWithException(ex);
            }
        }

        private async Task WriteSlowAsync<TMessage>(TMessage message, bool ignoreAbort, CancellationToken cancellationToken) where TMessage : RaidoMessage
        {
            // Failed to get the lock immediately when entering WriteAsync so await until it is available
            await _writeLock.WaitAsync(cancellationToken);
            try
            {
                if (ConnectionAborted.IsCancellationRequested && !ignoreAbort)
                {
                    return;
                }

                await CompleteWriteAsync(WriteCore(message, cancellationToken), cancellationToken);
            }
            finally
            {
                _writeLock.Release();
            }
        }



        public void Abort() => _tcpConnection.Abort();

        internal async Task AbortAsync()
        {
            var abortTask = _tcpConnection.AbortAsync();
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
            if (Debugger.IsAttached || !_tcpConnection.IsActive)
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

            if (timeoutException is not null && _tcpConnection.TryAbortIfActive(timeoutException))
            {
                Log.ClientTimeout(_logger, _clientTimeoutInterval);
                RaidoEventSource.Log.ConnectionTimedOut(ConnectionId);
            }
        }

        private void KeepAliveTick()
        {
            if (!_tcpConnection.IsActive)
            {
                return;
            }

            if (Features.Get<IConnectionInherentKeepAliveFeature>()?.HasInherentKeepAlive == true)
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
                    if (!_tcpConnection.IsActive)
                    {
                        return;
                    }

                    pingMessage = Protocol.GetMessageBytes(PingMessage.Instance);
                }
                catch (Exception ex)
                {
                    Log.FailedWritingMessage(_logger, ex);
                    _tcpConnection.AbortWithException(ex);
                    return;
                }

                try
                {
                    if (!_tcpConnection.TryWriteStableTransport(
                            output =>
                            {
                                var destination = output.GetSpan(pingMessage.Length);
                                pingMessage.Span.CopyTo(destination);
                                output.Advance(pingMessage.Length);
                            },
                            CancellationToken.None,
                            out _,
                            out var flushTask))
                    {
                        return;
                    }

                    await flushTask;
                    if (_tcpConnection.IsActive)
                    {
                        Log.SentPing(_logger);
                        // The ping was admitted successfully while the stable connection was active.
                        Volatile.Write(ref _lastSendTick, _timeProvider.GetTimestamp());
                    }
                }
                catch (OperationCanceledException ex)
                {
                    Log.FailedWritingMessage(_logger, ex);
                    _tcpConnection.AbortWithException(ex);
                }
                catch (IOException ex)
                {
                    Log.FailedWritingMessage(_logger, ex);
                    _tcpConnection.AbortWithException(ex);
                }
                catch (ObjectDisposedException ex)
                {
                    Log.FailedWritingMessage(_logger, ex);
                    _tcpConnection.AbortWithException(ex);
                }
                catch (Exception ex)
                {
                    Log.FailedWritingMessage(_logger, ex);
                    _tcpConnection.AbortWithException(ex);
                }
            }
            finally
            {
                _writeLock.Release();
            }
        }



        internal async Task CleanupAsync()
        {
            // Start lower cleanup first so it cancels pending stable flushes and quiesces relays while
            // the Hub write owner may still be holding this lock.
            var tcpCleanup = _tcpConnection.CleanupAsync();
            await _writeLock.WaitAsync().ConfigureAwait(false);
            var protocolLifetime = _protocolLifetime;
            _protocolLifetime = null;
            try
            {
                await tcpCleanup.ConfigureAwait(false);
                _tcpConnection.CompleteTransportOutput();
            }
            finally
            {
                try
                {
                    if (protocolLifetime is not null)
                    {
                        await protocolLifetime.DisposeAsync().ConfigureAwait(false);
                    }
                }
                finally
                {
                    _writeLock.Release();
                }
            }
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
