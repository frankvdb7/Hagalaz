using System;
using System.Threading.Tasks;
using Hagalaz.Services.GameWorld.Network.Handshake;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Raido.Server;

namespace Hagalaz.Services.GameWorld.Network;

public class ClientConnectionHandler : ConnectionHandler
{
    private readonly RaidoConnectionHandler _connectionHandler;
    private readonly IRaidoConnectionContextBuilder _contextBuilder;
    private readonly ILogger<ClientConnectionHandler> _logger;

    public ClientConnectionHandler(
        RaidoConnectionHandler connectionHandler,
        IRaidoConnectionContextBuilder contextBuilder,
        ILogger<ClientConnectionHandler> logger)
    {
        _connectionHandler = connectionHandler;
        _contextBuilder = contextBuilder;
        _logger = logger;
    }

    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        var connectionContext = _contextBuilder.Create().WithConnection(connection).WithProtocol<HandshakeProtocol>().Build();

        Log.HandshakeStart(_logger, connectionContext.Protocol.Name);

        await _connectionHandler.ConnectAsync(connectionContext);
    }

    private static class Log
    {
        private static readonly Action<ILogger, string, Exception?> _handshakeStart = LoggerMessage.Define<string>(LogLevel.Debug,
            new EventId(1, "HandshakeStart"),
            "Start connection handshake. Using protocol '{Protocol}'.");

        public static void HandshakeStart(ILogger logger, string protocol) => _handshakeStart(logger, protocol, null);

    }
}