using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Services.GameUpdate.Network.Messages;
using Hagalaz.Services.GameUpdate.Network.Protocol;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raido.Server;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameUpdate.Network
{
    public class UpdateConnectionHandler : ConnectionHandler
    {
        private readonly RaidoConnectionHandler _connectionHandler;
        private readonly IRaidoConnectionContextBuilder _contextBuilder;
        private readonly ILogger<UpdateConnectionHandler> _logger;
        private readonly IOptions<RaidoOptions> _raidoOptions;
        private readonly IOptions<ServerConfig> _serverOptions;

        public UpdateConnectionHandler(
            RaidoConnectionHandler connectionHandler,
            IRaidoConnectionContextBuilder contextBuilder,
            IOptions<RaidoOptions> raidoOptions,
            IOptions<ServerConfig> serverOptions,
            ILogger<UpdateConnectionHandler> logger)
        {
            _connectionHandler = connectionHandler;
            _contextBuilder = contextBuilder;
            _logger = logger;
            _raidoOptions = raidoOptions;
            _serverOptions = serverOptions;
        }

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            var protocol = new HandshakeProtocol();
            await using (var reader = connection.CreateReader())
            {
                await using (var writer = connection.CreateWriter())
                {
                    using (var cts = new CancellationTokenSource())
                    {
                        try
                        {
                            if (!Debugger.IsAttached)
                            {
                                cts.CancelAfter(TimeSpan.FromSeconds(10));
                            }

                            var raidoOptions = _raidoOptions.Value;
                            var result = await reader.ReadAsync(protocol, raidoOptions.MaximumReceiveMessageSize, cts.Token);
                            if (result.IsCanceled)
                            {
                                Log.HandshakeCanceled(_logger);
                                return;
                            }

                            var message = result.Message;
                            if (message == null || result.IsCompleted)
                            {
                                Log.HandshakeInvalid(_logger);
                                connection.Abort(new ConnectionAbortedException("Unknown handshake message."));
                                return;
                            }

                            var serverConfig = _serverOptions.Value;
                            if (message.ServerToken != serverConfig.ServerToken)
                            {
                                Log.HandshakeInvalid(_logger);
                                connection.Abort(new ConnectionAbortedException("Failed connection handshake."));
                                return;
                            }

                            if (message.ClientRevision != serverConfig.ClientRevision || message.ClientRevisionPatch != serverConfig.ClientRevisionPatch)
                            {
                                await writer.WriteAsync(protocol, HandshakeResponse.Outdated, cts.Token);
                                return;
                            }

                            connection.Items.Add(FileSyncSession.Key, new FileSyncSession());

                            await writer.WriteAsync(protocol, HandshakeResponse.Success(serverConfig.UpdateKeys), cts.Token);

                            reader.Advance(advanceCursor: true);
                        }
                        catch (OperationCanceledException)
                        {
                            Log.HandshakeCanceled(_logger);
                            connection.Abort(new ConnectionAbortedException("Handshake was cancelled."));
                            return;
                        }
                        catch (Exception ex)
                        {
                            Log.HandshakeFailed(_logger, ex);
                            connection.Abort(new ConnectionAbortedException("An unexpected error occurred during connection handshake."));
                            return;
                        }
                    }
                }
            }

            var connectionContext = _contextBuilder.Create().WithConnection(connection).WithProtocol<FileProtocol>().Build();

            Log.HandshakeComplete(_logger, connectionContext.Protocol.Name);

            await _connectionHandler.ConnectAsync(connectionContext);
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, Exception?> _handshakeComplete = LoggerMessage.Define<string>(LogLevel.Debug,
                new EventId(1, "HandshakeComplete"),
                "Completed connection handshake. Using Protocol '{Protocol}'.");

            private static readonly Action<ILogger, Exception?> _handshakeCanceled =
                LoggerMessage.Define(LogLevel.Debug, new EventId(2, "HandshakeCanceled"), "Handshake was canceled.");

            private static readonly Action<ILogger, Exception> _handshakeFailed =
                LoggerMessage.Define(LogLevel.Error, new EventId(3, "HandshakeFailed"), "Failed connection handshake.");

            private static readonly Action<ILogger, Exception?> _handshakeInvalid =
                LoggerMessage.Define(LogLevel.Warning, new EventId(4, "HandshakeInvalid"), "Invalid handshake token.");

            public static void HandshakeComplete(ILogger logger, string protocol) => _handshakeComplete(logger, protocol, null);

            public static void HandshakeCanceled(ILogger logger) => _handshakeCanceled(logger, null);

            public static void HandshakeFailed(ILogger logger, Exception exception) => _handshakeFailed(logger, exception);

            public static void HandshakeInvalid(ILogger logger) => _handshakeInvalid(logger, null);
        }
    }
}