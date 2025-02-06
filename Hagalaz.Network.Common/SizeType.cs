namespace Hagalaz.Network.Common
{
    /// <summary>
    /// Defines packet types that are noted by the client.
    /// </summary>
    public enum SizeType
    {
        /// <summary>
        /// A fixed size packet where the size never changes.
        /// </summary>
        Fixed,
        /// <summary>
        /// A variable packet where the size is described by a byte.
        /// </summary>
        Byte,
        /// <summary>
        /// A variable packet where the size is described by a short.
        /// </summary>
        Short
    }
}
