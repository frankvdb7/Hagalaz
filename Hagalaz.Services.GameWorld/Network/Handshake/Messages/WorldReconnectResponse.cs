using System;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Handshake.Messages
{
    public sealed class WorldReconnectResponse : RaidoMessage
    {
        public const int EnterWorldPayloadLength = 4608;

        public required ReadOnlyMemory<byte> EnterWorldPayload { get; init; }
    }
}
