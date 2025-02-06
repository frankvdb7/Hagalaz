using Raido.Common.Protocol;

namespace Hagalaz.Services.GameUpdate.Network.Messages
{
    public class HandshakeResponse : RaidoMessage
    {
        public static HandshakeResponse Success(int[] updateKeys) =>
            new()
            {
                Successful = true, UpdateKeys = updateKeys
            };

        public static HandshakeResponse Outdated { get; } = new()
        {
            Opcode = 6, IsOutdated = true
        };

        public static HandshakeResponse Expired { get; } = new()
        {
            Opcode = 48, IsExpired = true
        };

        public static HandshakeResponse ServiceUnavailable { get; } = new()
        {
            Opcode = 7, IsServiceUnavailable = true
        };

        public byte Opcode { get; private init; }
        public bool Successful { get; private init; }
        public bool IsOutdated { get; private init; }
        public bool IsExpired { get; private init; }
        public bool IsServiceUnavailable { get; private init; }
        public int[]? UpdateKeys { get; private init; }

        private HandshakeResponse()
        {
        }
    }
}