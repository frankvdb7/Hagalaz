using System;
using System.Threading.Tasks;
using Hagalaz.Services.GameWorld.Network.Handshake;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Raido.Server;

namespace Hagalaz.Services.GameWorld.Network;

public class ClientConnectionHandler : ConnectionHandler
{
    private readonly RaidoHubConnectionHandler _connectionHandler;
    private readonly IRaidoHubConnectionContextFactory _connectionFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ClientConnectionHandler> _logger;

    public ClientConnectionHandler(
        RaidoHubConnectionHandler connectionHandler,
        IRaidoHubConnectionContextFactory connectionFactory,
        IServiceScopeFactory scopeFactory,
        ILogger<ClientConnectionHandler> logger)
    {
        _connectionHandler = connectionHandler;
        _connectionFactory = connectionFactory;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var handshakeProtocol = scope.ServiceProvider.GetRequiredService<HandshakeProtocol>();
        var connectionContext = _connectionFactory.Create(connection, handshakeProtocol);

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
