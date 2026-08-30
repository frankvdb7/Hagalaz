using System;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Connections;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Raido.Server.Internal;

namespace Raido.Server
{
    /// <summary>
    /// Handles incoming Raido connections.
    /// </summary>
    public class RaidoConnectionHandler
    {
        private readonly IRaidoLifetimeManager _lifetimeManager;
        private readonly IRaidoDispatcher _dispatcher;
        private readonly RaidoMetrics _metrics;
        private readonly ILogger<RaidoConnectionHandler> _logger;
        private readonly long? _maximumReceiveMessageSize;
        private readonly TimeProvider _timeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="RaidoConnectionHandler"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="raidoOptions">The Raido options.</param>
        /// <param name="lifetimeManager">The lifetime manager.</param>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <param name="metrics">The metrics.</param>
        public RaidoConnectionHandler(
            ILoggerFactory loggerFactory, IOptions<RaidoOptions> raidoOptions,
            IRaidoLifetimeManager lifetimeManager, IRaidoDispatcher dispatcher, RaidoMetrics metrics)
        {
            _lifetimeManager = lifetimeManager;
            _dispatcher = dispatcher;
            _metrics = metrics;
            _logger = loggerFactory.CreateLogger<RaidoConnectionHandler>();
            _maximumReceiveMessageSize = raidoOptions.Value.MaximumReceiveMessageSize;
            _timeProvider = TimeProvider.System;
        }

        /// <summary>
        /// Handles a new connection.
        /// </summary>
        /// <param name="connection">The connection to handle.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous connection handling.</returns>
        public async Task ConnectAsync(RaidoConnectionContext connection)
        {
            connection.MetricsContext = _metrics.CreateContext();

            Log.ConnectedStarting(_logger, connection);
            RaidoEventSource.Log.ConnectionStart(connection.ConnectionId);
            _metrics.ConnectionStart(connection.MetricsContext);
            var acceptedPhysicalConnection = connection.TryGetCurrentConnection(out var currentConnection)
                ? currentConnection
                : null;
            var transferred = false;
            try
            {
                await connection.OnConnectedAsync();
                await _lifetimeManager.OnConnectedAsync(connection);
                transferred = await RunCoreAsync(connection);
            }
            finally
            {
                connection.Cleanup();

                var currentTimestamp = (connection.StartTimestamp > 0) ? _timeProvider.GetTimestamp() : default;

                Log.ConnectedStopping(_logger, connection);
                RaidoEventSource.Log.ConnectionStop(connection.ConnectionId, connection.StartTimestamp, currentTimestamp);
                _metrics.ConnectionStop(connection.MetricsContext, connection.StartTimestamp, currentTimestamp);
                await _lifetimeManager.OnDisconnectedAsync(connection);
            }

            if (transferred && acceptedPhysicalConnection is not null)
            {
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, acceptedPhysicalConnection.ConnectionClosed);
                }
                catch (OperationCanceledException) when (acceptedPhysicalConnection.ConnectionClosed.IsCancellationRequested)
                {
                    // The transferred physical connection has closed.
                }
            }
        }

        /// <summary>
        /// Runs the connection loop.
        /// </summary>
        /// <param name="connection">The connection to run.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous connection loop.</returns>
        public virtual async Task RunAsync(RaidoConnectionContext connection) => await RunCoreAsync(connection);

        private async Task<bool> RunCoreAsync(RaidoConnectionContext connection)
        {
            try
            {
                await _dispatcher.OnConnectedAsync(connection);
            }
            catch (Exception ex)
            {
                Log.ErrorDispatchingHubEvent(_logger, "OnConnectedAsync", ex);
                await OnDisconnectedAsync(connection, ex);
                return false;
            }

            var transferred = false;
            try
            {
                transferred = await DispatchMessagesCoreAsync(connection);
            }
            catch (OperationCanceledException) when (connection.IsTerminal)
            {
                // Terminal cancellation is control flow used to stop transport operations.
            }
            catch (Exception ex)
            {
                Log.ErrorProcessingRequest(_logger, ex);
                await OnDisconnectedAsync(connection, ex);
                return false;
            }

            if (!transferred)
            {
                await OnDisconnectedAsync(connection, connection.CloseException);
            }
            else
            {
                // The transferred transport is no longer owned by this context before its
                // normal disconnect callback performs the usual abort synchronization.
                connection.Cleanup();
                await OnDisconnectedAsync(connection, connection.CloseException);
            }

            return transferred;
        }

        /// <summary>
        /// Dispatches messages from the connection.
        /// </summary>
        /// <param name="connection">The connection to dispatch messages from.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous message dispatching.</returns>
        public virtual async Task DispatchMessagesAsync(RaidoConnectionContext connection) => await DispatchMessagesCoreAsync(connection);

        private async Task<bool> DispatchMessagesCoreAsync(RaidoConnectionContext connection)
        {
            while (!connection.IsTerminal)
            {
                if (!connection.TryGetCurrentConnection(out var physicalConnection))
                {
                    if (!connection.IsReconnectEnabled || !await connection.WaitForReconnectAsync())
                    {
                        break;
                    }

                    continue;
                }

                await using (var protocolReader = new RaidoProtocolReader(physicalConnection.Transport.Input))
                {
                    while (true)
                    {
                        RaidoProtocolReadResult<RaidoMessage> result;
                        try
                        {
                            connection.BeginClientTimeout();
                            result = await protocolReader.ReadAsync(connection.Protocol, _maximumReceiveMessageSize, connection.ConnectionAbortedToken);
                        }
                        catch (OperationCanceledException ex)
                        {
                            if (connection.IsTerminal)
                            {
                                break;
                            }

                            if (!protocolReader.IsPhysicalTransportFailure(ex) ||
                                !connection.HandleTransportFailure(physicalConnection, ex))
                            {
                                throw;
                            }

                            break;
                        }
                        catch (IOException ex)
                        {
                            if (!protocolReader.IsPhysicalTransportFailure(ex) ||
                                !connection.HandleTransportFailure(physicalConnection, ex))
                            {
                                throw;
                            }

                            break;
                        }

                        var dispatchCompleted = false;
                        var transferred = false;
                        Func<bool>? postDispatchAction = null;
                        try
                        {
                            if (result.IsCanceled)
                            {
                                break;
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
                            dispatchCompleted = true;

                            if (result.IsCompleted)
                            {
                                break;
                            }
                        }
                        finally
                        {
                            if (dispatchCompleted && (postDispatchAction = connection.TakePostDispatchAction()) is not null)
                            {
                                protocolReader.Advance(true);
                                transferred = postDispatchAction();
                            }
                            else
                            {
                                protocolReader.Advance();
                            }
                        }

                        if (postDispatchAction is not null)
                        {
                            if (transferred)
                            {
                                return true;
                            }

                            connection.Abort();
                            return false;
                        }
                    }
                }

                connection.OnPhysicalConnectionClosed(physicalConnection);
            }

            return false;
        }

        /// <summary>
        /// Handles a connection disconnect.
        /// </summary>
        /// <param name="connection">The connection that disconnected.</param>
        /// <param name="exception">The exception that caused the disconnect, if any.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous disconnect handling.</returns>
        public virtual async Task OnDisconnectedAsync(RaidoConnectionContext connection, Exception? exception)
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

            public static void ConnectedStarting(ILogger logger, RaidoConnectionContext connectionContext) =>
                _connectedStarting(logger, connectionContext.ConnectionId, null);

            public static void ConnectedStopping(ILogger logger, RaidoConnectionContext connectionContext) =>
                _connectedStopping(logger, connectionContext.ConnectionId, null);

            public static void ReceivedMessage(ILogger logger, RaidoMessage message) => _receivedMessage(logger, message.GetType().Name, null);
        }
    }
}
