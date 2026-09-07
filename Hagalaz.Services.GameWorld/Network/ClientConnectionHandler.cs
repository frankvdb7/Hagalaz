using System;
using System.Buffers;
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

internal sealed class ClientConnectionHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly HandshakeProtocol _handshakeProtocol;
    private readonly IOptions<RaidoOptions> _raidoOptions;
    private readonly ILogger<ClientConnectionHandler> _logger;

    public ClientConnectionHandler(
        IServiceProvider serviceProvider,
        HandshakeProtocol handshakeProtocol,
        IOptions<RaidoOptions> raidoOptions,
        ILogger<ClientConnectionHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _handshakeProtocol = handshakeProtocol;
        _raidoOptions = raidoOptions;
        _logger = logger;
    }

    public async Task HandleAsync(
        ConnectionContext connection,
        RaidoConnectionDispatchContext dispatch,
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
                isValidOpcode: static opcode => opcode == 14);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            Log.HandshakeFailed(_logger, ex);
            return;
        }

        if (handshake is not ClientHandshakeRequest)
        {
            return;
        }

        try
        {
            await SendHandshakeResponseAsync(
                connection,
                _handshakeProtocol,
                new ClientHandshakeResponse { ReturnCode = 0 },
                handshakeCancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            Log.HandshakeFailed(_logger, ex);
            return;
        }

        byte? authenticationOpcode;
        try
        {
            authenticationOpcode = await ReadAuthenticationOpcodeAsync(
                connection,
                _raidoOptions.Value.MaximumReceiveMessageSize,
                handshakeCancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            Log.HandshakeFailed(_logger, ex);
            return;
        }

        if (authenticationOpcode is null)
        {
            return;
        }

        if (authenticationOpcode == 19)
        {
            await DispatchNewAsync(dispatch, statefulReconnect: false, handshakeCancellationToken);
            return;
        }

        bool? isReconnect;
        try
        {
            isReconnect = await ReadWorldReconnectFlagAsync(
                connection,
                _raidoOptions.Value.MaximumReceiveMessageSize,
                handshakeCancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            Log.HandshakeFailed(_logger, ex);
            return;
        }

        if (isReconnect is null)
        {
            return;
        }

        if (!isReconnect.Value)
        {
            await DispatchNewAsync(dispatch, statefulReconnect: true, handshakeCancellationToken);
            return;
        }

        RaidoMessage? authentication;
        try
        {
            authentication = await ReadMessageAsync(
                connection,
                _handshakeProtocol,
                _raidoOptions.Value.MaximumReceiveMessageSize,
                handshakeCancellationToken,
                isValidOpcode: static opcode => opcode == 16);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            Log.HandshakeFailed(_logger, ex);
            return;
        }

        if (authentication is WorldReconnectRequest reconnectRequest)
        {
            await _serviceProvider.GetRequiredService<WorldReconnectConnectionHandler>().HandleAsync(
                connection,
                dispatch,
                _handshakeProtocol,
                reconnectRequest,
                handshakeCancellationToken);
            return;
        }

        return;
    }

    private async ValueTask DispatchNewAsync(
        RaidoConnectionDispatchContext dispatch,
        bool statefulReconnect,
        CancellationToken handshakeCancellationToken)
    {
        try
        {
            await dispatch.DispatchNewAsync(
                _handshakeProtocol,
                statefulReconnect,
                handshakeCancellationToken);
        }
        catch (OperationCanceledException) when (handshakeCancellationToken.IsCancellationRequested)
        {
        }
    }

    internal static async ValueTask<RaidoMessage?> ReadMessageAsync(
        ConnectionContext connection,
        HandshakeProtocol protocol,
        long? maximumMessageSize,
        CancellationToken cancellationToken,
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
                var advanceTo = consumed;
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

    private static async ValueTask<byte?> ReadAuthenticationOpcodeAsync(
        ConnectionContext connection,
        long? maximumMessageSize,
        CancellationToken cancellationToken)
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

            if (!buffer.IsEmpty)
            {
                var opcode = buffer.FirstSpan[0];
                if (opcode is not 16 and not 19)
                {
                    connection.Transport.Input.AdvanceTo(buffer.End);
                    throw new InvalidDataException("The handshake opcode was invalid.");
                }

                connection.Transport.Input.AdvanceTo(buffer.Start, buffer.Start);
                return opcode;
            }

            if (result.IsCompleted)
            {
                connection.Transport.Input.AdvanceTo(buffer.End);
                return null;
            }

            connection.Transport.Input.AdvanceTo(buffer.Start, buffer.End);
        }
    }

    private static async ValueTask<bool?> ReadWorldReconnectFlagAsync(
        ConnectionContext connection,
        long? maximumMessageSize,
        CancellationToken cancellationToken)
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

            if (!buffer.IsEmpty && TryReadWorldReconnectFlag(buffer.Slice(1), out var isReconnect))
            {
                connection.Transport.Input.AdvanceTo(buffer.Start, buffer.Start);
                return isReconnect;
            }

            if (result.IsCompleted)
            {
                connection.Transport.Input.AdvanceTo(buffer.End);
                return null;
            }

            connection.Transport.Input.AdvanceTo(buffer.Start, buffer.End);
        }
    }

    internal static bool TryReadWorldReconnectFlag(
        in ReadOnlySequence<byte> payload,
        out bool isReconnect)
    {
        isReconnect = false;
        var reader = new SequenceReader<byte>(payload);
        if (!reader.TryReadBigEndian(out short packetSize))
        {
            return false;
        }

        if (packetSize < 0)
        {
            throw new InvalidDataException("The world authentication packet size was invalid.");
        }

        if (reader.Remaining < packetSize)
        {
            return false;
        }

        if (reader.Remaining != packetSize || packetSize < 9)
        {
            throw new InvalidDataException("The world authentication packet framing was invalid.");
        }

        reader.Advance(8);
        if (!reader.TryRead(out byte reconnectFlag))
        {
            throw new InvalidDataException("The world authentication packet header was invalid.");
        }

        isReconnect = reconnectFlag == 1;
        return true;
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

        if (result.IsCompleted)
        {
            throw new ConnectionAbortedException(
                "The client connection completed while sending the handshake response.");
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
