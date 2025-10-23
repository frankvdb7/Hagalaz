namespace Hagalaz.Network.Common
{
    /// <summary>
    /// Defines the contract for a packet composer, which is responsible for serializing a network message into a byte array.
    /// </summary>
    public interface IPacketComposer
    {
        /// <summary>
        /// Converts the entire packet, including its headers and payload, into a byte array for transmission.
        /// </summary>
        /// <returns>A byte array representing the serialized packet.</returns>
        byte[] Serialize();
    }
}
