using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raido.Common.Protocol;
using Raido.Server.Extensions;
using Raido.Server.Internal;

namespace Raido.Server
{
    public class RaidoConnectionHandler
    {
        private readonly IRaidoLifetimeManager _lifetimeManager;
        private readonly IRaidoDispatcher _dispatcher;
        private readonly RaidoMetrics _metrics;
        private readonly ILogger<RaidoConnectionHandler> _logger;
        private readonly long? _maximumReceiveMessageSize;
        private readonly TimeProvider _timeProvider;

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

        public async Task ConnectAsync(RaidoConnectionContext connection)
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
                connection.Cleanup();

                var currentTimestamp = (connection.StartTimestamp > 0) ? _timeProvider.GetTimestamp() : default;

                Log.ConnectedStopping(_logger, connection);
                RaidoEventSource.Log.ConnectionStop(connection.ConnectionId, connection.StartTimestamp, currentTimestamp);
                _metrics.ConnectionStop(connection.MetricsContext, connection.StartTimestamp, currentTimestamp);
                await _lifetimeManager.OnDisconnectedAsync(connection);
            }
        }

        public virtual async Task RunAsync(RaidoConnectionContext connection)
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
            catch (OperationCanceledException)
            {
                // Don't treat OperationCanceledException as an error, it's basically a "control flow"
                // exception to stop things from running
            }
            catch (Exception ex)
            {
                Log.ErrorProcessingRequest(_logger, ex);
                await OnDisconnectedAsync(connection, ex);
                return;
            }

            await OnDisconnectedAsync(connection, connection.CloseException);
        }

        public virtual async Task DispatchMessagesAsync(RaidoConnectionContext connection)
        {
            await using (var protocolReader = connection.CreateReader())
            {
                while (true)
                {
                    try
                    {
                        connection.BeginClientTimeout();
                        var result = await protocolReader.ReadAsync(connection.Protocol, _maximumReceiveMessageSize, connection.ConnectionAbortedToken);
                        if (result.IsCanceled)
                        {
                            break;
                        }

                        if (result.Message == default)
                        {
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
                        protocolReader.Advance();
                    }
                }
            }
        }

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