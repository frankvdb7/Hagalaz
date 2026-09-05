using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Raido.Server.Internal;

namespace Raido.Server
{
    /// <summary>
    /// Handles incoming Raido Hub connections.
    /// </summary>
    public class RaidoHubConnectionHandler
    {
        private readonly IRaidoHubLifetimeManager _lifetimeManager;
        private readonly IRaidoDispatcher _dispatcher;
        private readonly RaidoMetrics _metrics;
        private readonly ILogger<RaidoHubConnectionHandler> _logger;
        private readonly long? _maximumReceiveMessageSize;
        private readonly TimeProvider _timeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="RaidoHubConnectionHandler"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="raidoOptions">The Raido options.</param>
        /// <param name="lifetimeManager">The lifetime manager.</param>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <param name="metrics">The metrics.</param>
        public RaidoHubConnectionHandler(
            ILoggerFactory loggerFactory, IOptions<RaidoOptions> raidoOptions,
            IRaidoHubLifetimeManager lifetimeManager, IRaidoDispatcher dispatcher, RaidoMetrics metrics)
        {
            _lifetimeManager = lifetimeManager;
            _dispatcher = dispatcher;
            _metrics = metrics;
            _logger = loggerFactory.CreateLogger<RaidoHubConnectionHandler>();
            _maximumReceiveMessageSize = raidoOptions.Value.MaximumReceiveMessageSize;
            _timeProvider = TimeProvider.System;
        }

        /// <summary>
        /// Handles a new connection.
        /// </summary>
        /// <param name="connection">The connection to handle.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous connection handling.</returns>
        public async Task ConnectAsync(RaidoHubConnectionContext connection)
        {
            connection.MetricsContext = _metrics.CreateContext();

            Log.ConnectedStarting(_logger, connection);
            RaidoEventSource.Log.ConnectionStart(connection.ConnectionId);
            _metrics.ConnectionStart(connection.MetricsContext);
            try
            {
                await connection.OnConnectedAsync();
                await _lifetimeManager.OnConnectedAsync(connection);
                await RunAsync(connection);
            }
            finally
            {
                ExceptionDispatchInfo? cleanupException = null;
                try
                {
                    connection.CompleteTransportInput();
                    await connection.CleanupAsync();
                }
                catch (Exception ex)
                {
                    cleanupException = ExceptionDispatchInfo.Capture(ex);
                }

                var currentTimestamp = (connection.StartTimestamp > 0) ? _timeProvider.GetTimestamp() : default;

                Log.ConnectedStopping(_logger, connection);
                RaidoEventSource.Log.ConnectionStop(connection.ConnectionId, connection.StartTimestamp, currentTimestamp);
                _metrics.ConnectionStop(connection.MetricsContext, connection.StartTimestamp, currentTimestamp);

                try
                {
                    await _lifetimeManager.OnDisconnectedAsync(connection);
                }
                finally
                {
                    cleanupException?.Throw();
                }
            }
        }

        /// <summary>
        /// Activates a physical connection on an existing logical Raido connection.
        /// </summary>
        /// <param name="connection">The existing logical connection.</param>
        /// <param name="physicalConnection">The physical connection to activate.</param>
        /// <returns><see langword="true"/> when the physical connection was accepted.</returns>
        public bool TryActivatePhysicalConnection(
            RaidoHubConnectionContext connection,
            ConnectionContext physicalConnection)
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentNullException.ThrowIfNull(physicalConnection);
            return connection.TryAttachPhysicalConnection(physicalConnection);
        }

        /// <summary>
        /// Gets whether the logical connection is currently detached within its existing reconnect window.
        /// </summary>
        /// <param name="connection">The existing logical connection.</param>
        /// <returns><see langword="true"/> when a replacement physical connection can currently be considered.</returns>
        public bool IsReconnectable(RaidoHubConnectionContext connection)
        {
            ArgumentNullException.ThrowIfNull(connection);
            return connection.IsReconnectable;
        }

        /// <summary>
        /// Runs the connection loop.
        /// </summary>
        /// <param name="connection">The connection to run.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous connection loop.</returns>
        private async Task RunAsync(RaidoHubConnectionContext connection)
        {
            try
            {
                await _dispatcher.OnConnectedAsync(connection);
            }
            catch (Exception ex)
            {
                Log.ErrorDispatchingHubEvent(_logger, "OnConnectedAsync", ex);
                await OnDisconnectedAsync(connection, ex);
                return;
            }

            try
            {
                await DispatchMessagesAsync(connection);
            }
            catch (OperationCanceledException) when (connection.IsTerminal)
            {
                // Terminal cancellation is control flow used to stop transport operations.
            }
            catch (Exception ex)
            {
                Log.ErrorProcessingRequest(_logger, ex);
                await OnDisconnectedAsync(connection, ex);
                return;
            }

            await OnDisconnectedAsync(connection, connection.TerminalException);
        }

        /// <summary>
        /// Dispatches messages from the connection.
        /// </summary>
        /// <param name="connection">The connection to dispatch messages from.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous message dispatching.</returns>
        private async Task DispatchMessagesAsync(RaidoHubConnectionContext connection)
        {
            var protocolReader = new RaidoProtocolReader(connection.TransportInput);
            try
            {
                while (!connection.IsTerminal)
                {
                    RaidoProtocolReadResult<RaidoMessage> result;
                    try
                    {
                        connection.BeginClientTimeout();
                        result = await protocolReader.ReadAsync(connection.Protocol, _maximumReceiveMessageSize, connection.ConnectionAborted);
                    }
                    catch (OperationCanceledException) when (connection.IsTerminal)
                    {
                        break;
                    }

                    var discardInput = result.IsCanceled;
                    try
                    {
                        if (result.IsCanceled)
                        {
                            connection.StopClientTimeout();
                            if (connection.IsTerminal)
                            {
                                break;
                            }

                            while (protocolReader.TryReadBufferedMessage(
                                       connection.Protocol,
                                       _maximumReceiveMessageSize,
                                       out var bufferedResult))
                            {
                                if (bufferedResult.Message != default)
                                {
                                    connection.StopClientTimeout();
                                    Log.ReceivedMessage(_logger, bufferedResult.Message);
                                    await _dispatcher.DispatchMessageAsync(connection, bufferedResult.Message);
                                }

                                protocolReader.Advance();
                            }

                            if (!await connection.WaitForReconnectAsync().ConfigureAwait(false))
                            {
                                break;
                            }

                            continue;
                        }

                        if (result.Message == default)
                        {
                            if (result.IsCompleted)
                            {
                                break;
                            }

                            continue;
                        }

                        connection.StopClientTimeout();

                        Log.ReceivedMessage(_logger, result.Message);

                        await _dispatcher.DispatchMessageAsync(connection, result.Message);

                        if (result.IsCompleted)
                        {
                            break;
                        }
                    }
                    finally
                    {
                        try
                        {
                            if (discardInput)
                            {
                                protocolReader.DiscardIncompleteInput();
                            }
                            else
                            {
                                protocolReader.Advance();
                            }
                        }
                        finally
                        {
                            if (discardInput)
                            {
                                connection.AcknowledgeInputBoundary();
                            }
                        }
                    }
                }
            }
            finally
            {
                await protocolReader.DisposeAsync();
                connection.CompleteTransportInput();
            }
        }

        /// <summary>
        /// Handles a connection disconnect.
        /// </summary>
        /// <param name="connection">The connection that disconnected.</param>
        /// <param name="exception">The exception that caused the disconnect, if any.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous disconnect handling.</returns>
        private async Task OnDisconnectedAsync(RaidoHubConnectionContext connection, Exception? exception)
        {
            // We wait on abort to complete, this is so that we can guarantee that all callbacks have fired
            // before OnDisconnectedAsync

            // Ensure the connection is aborted before firing disconnect
            await connection.AbortAsync();

            try
            {
                await _dispatcher.OnDisconnectedAsync(connection, exception);
            }
            catch (Exception ex)
            {
                Log.ErrorDispatchingHubEvent(_logger, "OnDisconnectedAsync", ex);
                throw;
            }
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, Exception> _errorDispatchingEvent = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(1, "ErrorDispatchingEvent"),
                "Error when dispatching '{Method}' on hub.");

            private static readonly Action<ILogger, Exception> _errorProcessingRequest =
                LoggerMessage.Define(LogLevel.Error,
                    new EventId(2, "ErrorProcessingRequest"),
                    "Error while processing requests.");

            private static readonly Action<ILogger, string, Exception?> _connectedStarting =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(3, "ConnectedStarting"), "OnConnectedAsync started for connection '{ConnectionId}'.");

            private static readonly Action<ILogger, string, Exception?> _connectedStopping =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(4, "ConnectedStopping"), "OnConnectedAsync stopping for connection '{ConnectionId}'.");

            private static readonly Action<ILogger, string, Exception?> _receivedMessage =
                LoggerMessage.Define<string>(LogLevel.Trace, new EventId(5, "ReceivedMessage"), "Received a {MessageName} from the client.");

            public static void ErrorDispatchingHubEvent(ILogger logger, string method, Exception exception) =>
                _errorDispatchingEvent(logger, method, exception);

            public static void ErrorProcessingRequest(ILogger logger, Exception exception) => _errorProcessingRequest(logger, exception);

            public static void ConnectedStarting(ILogger logger, RaidoHubConnectionContext connectionContext) =>
                _connectedStarting(logger, connectionContext.ConnectionId, null);

            public static void ConnectedStopping(ILogger logger, RaidoHubConnectionContext connectionContext) =>
                _connectedStopping(logger, connectionContext.ConnectionId, null);

            public static void ReceivedMessage(ILogger logger, RaidoMessage message) => _receivedMessage(logger, message.GetType().Name, null);
        }
    }
}
