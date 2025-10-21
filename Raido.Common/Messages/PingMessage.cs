using Raido.Common.Protocol;

namespace Raido.Common.Messages
{
    /// <summary>
    /// A ping message.
    /// </summary>
    public class PingMessage : RaidoMessage
    {
        /// <summary>
        /// Gets the singleton instance of the <see cref="PingMessage"/>.
        /// </summary>
        public static PingMessage Instance { get; } = new();

        private PingMessage() {}
    }
}