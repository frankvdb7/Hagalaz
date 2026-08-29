using System;
using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Raido.Common.Protocol;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Handshake.Encoders
{
    public sealed class WorldReconnectResponseEncoder : IRaidoMessageEncoder<WorldReconnectResponse>
    {
        public void EncodeMessage(WorldReconnectResponse message, IRaidoMessageBinaryWriter output)
        {
            if (message.EnterWorldPayload.Length != WorldReconnectResponse.EnterWorldPayloadLength)
            {
                throw new InvalidOperationException(
                    $"The reconnect enter-world payload must be {WorldReconnectResponse.EnterWorldPayloadLength} bytes.");
            }

            output
                .SetOpcode(15)
                .SetSize(RaidoMessageSize.VariableShort)
                .Write(message.EnterWorldPayload.Span);
        }
    }
}
