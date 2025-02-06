namespace Hagalaz.Network.Common
{
    /// <summary>
    /// Represents a general use packet composer.
    /// </summary>
    public class GenericPacketComposer : PacketComposer
    {
        #region Constructors
        /// <summary>
        /// Constructs a new general use composer with a default capacity base.
        /// </summary>
        public GenericPacketComposer() : base() { }

        /// <summary>
        /// Constructs a new general use composer with a specified capacity base.
        /// </summary>
        public GenericPacketComposer(int capacity) : base(capacity) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericPacketComposer"/> class.
        /// </summary>
        /// <param name="payload">The payload.</param>
        public GenericPacketComposer(byte[] payload) : base(payload) { }
        #endregion Constructors
    }
}
