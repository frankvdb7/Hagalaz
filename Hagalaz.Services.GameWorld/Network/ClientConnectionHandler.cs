using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Services.GameWorld.Network.Handshake;
using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raido.Common.Messages;
using Raido.Common.Protocol;
using Raido.Server;

namespace Hagalaz.Services.GameWorld.Network;

public class ClientConnectionHandler : ConnectionHandler
{
    private readonly RaidoHubConnectionHandler _connectionHandler;
    private readonly IRaidoHubConnectionContextFactory _connectionFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WorldReconnectConnectionHandler _reconnectHandler;
    private readonly IOptions<RaidoOptions> _raidoOptions;
    private readonly ILogger<ClientConnectionHandler> _logger;

    public ClientConnectionHandler(
        RaidoHubConnectionHandler connectionHandler,
        IRaidoHubConnectionContextFactory connectionFactory,
        IServiceScopeFactory scopeFactory,
        WorldReconnectConnectionHandler reconnectHandler,
        IOptions<RaidoOptions> raidoOptions,
        ILogger<ClientConnectionHandler> logger)
    {
        _connectionHandler = connectionHandler;
        _connectionFactory = connectionFactory;
        _scopeFactory = scopeFactory;
        _reconnectHandler = reconnectHandler;
        _raidoOptions = raidoOptions;
        _logger = logger;
    }

    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var handshakeProtocol = scope.ServiceProvider.GetRequiredService<HandshakeProtocol>();
        using var cancellation = new CancellationTokenSource();
        if (!System.Diagnostics.Debugger.IsAttached)
        {
            cancellation.CancelAfter(TimeSpan.FromSeconds(10));
        }

        RaidoMessage? handshake;
        try
        {
            handshake = await ReadMessageAsync(
                connection,
                handshakeProtocol,
                _raidoOptions.Value.MaximumReceiveMessageSize,
                cancellation.Token,
                shouldConsume: static _ => true,
                isValidOpcode: static opcode => opcode == 14);
        }
        catch (OperationCanceledException)
        {
            connection.Abort(new ConnectionAbortedException("The connection handshake was canceled."));
            return;
        }
        catch (Exception ex)
        {
            connection.Abort(new ConnectionAbortedException("The connection handshake was invalid.", ex));
            return;
        }

        if (handshake is not ClientHandshakeRequest)
        {
            connection.Abort(new ConnectionAbortedException("The client handshake was invalid."));
            return;
        }

        try
        {
            await SendHandshakeResponseAsync(connection, handshakeProtocol, cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            connection.Abort(new ConnectionAbortedException("The handshake response was canceled."));
            return;
        }
        catch (Exception ex)
        {
            connection.Abort(new ConnectionAbortedException("The handshake response could not be sent.", ex));
            return;
        }

        RaidoMessage? authentication;
        try
        {
            authentication = await ReadMessageAsync(
                connection,
                handshakeProtocol,
                _raidoOptions.Value.MaximumReceiveMessageSize,
                cancellation.Token,
                shouldConsume: static message => message is WorldReconnectRequest,
                isValidOpcode: static opcode => opcode is 16 or 19);
        }
        catch (OperationCanceledException)
        {
            connection.Abort(new ConnectionAbortedException("The authentication handshake was canceled."));
            return;
        }
        catch (Exception ex)
        {
            connection.Abort(new ConnectionAbortedException("The authentication handshake was invalid.", ex));
            return;
        }

        if (authentication is null)
        {
            connection.Abort(new ConnectionAbortedException("Unknown authentication handshake message."));
            return;
        }

        if (authentication is WorldReconnectRequest reconnectRequest)
        {
            await _reconnectHandler.HandleAsync(connection, handshakeProtocol, reconnectRequest, cancellation.Token);
            return;
        }

        var connectionContext = _connectionFactory.Create(
            connection,
            handshakeProtocol,
            statefulReconnect: authentication is WorldSignInRequest);

        Log.HandshakeStart(_logger, connectionContext.Protocol.Name);

        await _connectionHandler.ConnectAsync(connectionContext);
    }

    internal static async ValueTask<RaidoMessage?> ReadMessageAsync(
        ConnectionContext connection,
        HandshakeProtocol protocol,
        long? maximumMessageSize,
        CancellationToken cancellationToken,
        Func<RaidoMessage, bool> shouldConsume,
        Func<byte, bool>? isValidOpcode = null)
    {
        while (true)
        {
            var result = await connection.Transport.Input.ReadAsync(cancellationToken);
            var buffer = result.Buffer;
            if (result.IsCanceled)
            {
                connection.Transport.Input.AdvanceTo(buffer.Start, buffer.Start);
                return null;
            }

            if (maximumMessageSize is long maximum && buffer.Length > maximum)
            {
                connection.Transport.Input.AdvanceTo(buffer.End);
                throw new InvalidDataException($"The maximum message size of {maximum}B was exceeded.");
            }

            var consumed = buffer.Start;
            var examined = buffer.End;
            if (isValidOpcode is not null && !buffer.IsEmpty && !isValidOpcode(buffer.FirstSpan[0]))
            {
                connection.Transport.Input.AdvanceTo(buffer.End);
                throw new InvalidDataException("The handshake opcode was invalid.");
            }

            if (protocol.TryParseMessage(buffer, ref consumed, ref examined, out var message) && message is not null)
            {
                var advanceTo = shouldConsume(message) ? consumed : buffer.Start;
                if (message is ClientHandshakeRequest)
                {
                    advanceTo = buffer.GetPosition(1);
                }
                connection.Transport.Input.AdvanceTo(advanceTo, advanceTo);
                return message;
            }

            if (result.IsCompleted)
            {
                connection.Transport.Input.AdvanceTo(buffer.End);
                return null;
            }

            connection.Transport.Input.AdvanceTo(buffer.Start, buffer.End);
        }
    }

    private static async Task SendHandshakeResponseAsync(
        ConnectionContext connection,
        HandshakeProtocol protocol,
        CancellationToken cancellationToken)
    {
        protocol.WriteMessage(
            new ClientHandshakeResponse { ReturnCode = 0 },
            connection.Transport.Output);
        var result = await connection.Transport.Output.FlushAsync(cancellationToken);
        if (result.IsCanceled)
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private static class Log
    {
        private static readonly Action<ILogger, string, Exception?> _handshakeStart = LoggerMessage.Define<string>(LogLevel.Debug,
            new EventId(1, "HandshakeStart"),
            "Start connection handshake. Using protocol '{Protocol}'.");

        public static void HandshakeStart(ILogger logger, string protocol) => _handshakeStart(logger, protocol, null);

    }
}
