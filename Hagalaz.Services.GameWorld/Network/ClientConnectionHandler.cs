using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Services.GameWorld.Network.Handshake;
using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raido.Common.Messages;
using Raido.Common.Protocol;
using Raido.Server;

namespace Hagalaz.Services.GameWorld.Network;

public class ClientConnectionHandler
{
    private readonly WorldReconnectConnectionHandler _reconnectHandler;
    private readonly HandshakeProtocol _handshakeProtocol;
    private readonly IClientHandshakeHandler _clientHandshakeHandler;
    private readonly IOptions<RaidoOptions> _raidoOptions;
    private readonly ILogger<ClientConnectionHandler> _logger;

    public ClientConnectionHandler(
        WorldReconnectConnectionHandler reconnectHandler,
        HandshakeProtocol handshakeProtocol,
        IClientHandshakeHandler clientHandshakeHandler,
        IOptions<RaidoOptions> raidoOptions,
        ILogger<ClientConnectionHandler> logger)
    {
        _reconnectHandler = reconnectHandler;
        _handshakeProtocol = handshakeProtocol;
        _clientHandshakeHandler = clientHandshakeHandler;
        _raidoOptions = raidoOptions;
        _logger = logger;
    }

    public async ValueTask<RaidoConnectionSelection> SelectAsync(
        ConnectionContext connection,
        CancellationToken cancellationToken)
    {
        using var cancellation = new CancellationTokenSource();
        if (!System.Diagnostics.Debugger.IsAttached)
        {
            cancellation.CancelAfter(TimeSpan.FromSeconds(10));
        }
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellation.Token,
            cancellationToken);
        var handshakeCancellationToken = linkedCancellation.Token;

        RaidoMessage? handshake;
        try
        {
            handshake = await ReadMessageAsync(
                connection,
                _handshakeProtocol,
                _raidoOptions.Value.MaximumReceiveMessageSize,
                handshakeCancellationToken,
                shouldConsume: static _ => true,
                isValidOpcode: static opcode => opcode == 14);
        }
        catch (OperationCanceledException)
        {
            return RaidoConnectionSelection.Rejected();
        }
        catch (Exception ex)
        {
            Log.HandshakeFailed(_logger, ex);
            return RaidoConnectionSelection.Rejected();
        }

        if (handshake is not ClientHandshakeRequest)
        {
            return RaidoConnectionSelection.Rejected();
        }

        try
        {
            var handshakeResponse = _clientHandshakeHandler.Handle((ClientHandshakeRequest)handshake);
            await SendHandshakeResponseAsync(connection, _handshakeProtocol, handshakeResponse, handshakeCancellationToken);
            if (handshakeResponse.ReturnCode != 0)
            {
                return RaidoConnectionSelection.Rejected();
            }
        }
        catch (OperationCanceledException)
        {
            return RaidoConnectionSelection.Rejected();
        }
        catch (Exception ex)
        {
            Log.HandshakeFailed(_logger, ex);
            return RaidoConnectionSelection.Rejected();
        }

        RaidoMessage? authentication;
        try
        {
            authentication = await ReadMessageAsync(
                connection,
                _handshakeProtocol,
                _raidoOptions.Value.MaximumReceiveMessageSize,
                handshakeCancellationToken,
                shouldConsume: static message => message is WorldReconnectRequest,
                isValidOpcode: static opcode => opcode is 16 or 19);
        }
        catch (OperationCanceledException)
        {
            return RaidoConnectionSelection.Rejected();
        }
        catch (Exception ex)
        {
            Log.HandshakeFailed(_logger, ex);
            return RaidoConnectionSelection.Rejected();
        }

        if (authentication is null)
        {
            return RaidoConnectionSelection.Rejected();
        }

        if (authentication is WorldReconnectRequest reconnectRequest)
        {
            return await _reconnectHandler.SelectAsync(
                connection,
                _handshakeProtocol,
                reconnectRequest,
                handshakeCancellationToken);
        }

        return RaidoConnectionSelection.New(
            _handshakeProtocol,
            statefulReconnect: authentication is WorldSignInRequest);
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
        ClientHandshakeResponse response,
        CancellationToken cancellationToken)
    {
        protocol.WriteMessage(response, connection.Transport.Output);
        var result = await connection.Transport.Output.FlushAsync(cancellationToken);
        if (result.IsCanceled)
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private static class Log
    {
        private static readonly Action<ILogger, Exception> _handshakeFailed = LoggerMessage.Define(
            LogLevel.Debug,
            new EventId(1, "HandshakeFailed"),
            "Client handshake failed.");

        public static void HandshakeFailed(ILogger logger, Exception exception) => _handshakeFailed(logger, exception);

    }
}
