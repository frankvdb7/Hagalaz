using Raido.Common.Protocol;

namespace Raido.Common.Messages
{
    public class PingMessage : RaidoMessage
    {
        public static PingMessage Instance { get; } = new();

        private PingMessage() {}
    }
}