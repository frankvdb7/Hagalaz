using System;
using Raido.Common.Protocol;

namespace Raido.Server
{
    /// <summary>
    /// A factory for creating message decoders and encoders for a specific protocol.
    /// </summary>
    /// <typeparam name="TProtocol">The type of the protocol.</typeparam>
    public interface IRaidoCodecFactory<TProtocol> where TProtocol : IRaidoProtocol
    {
        /// <summary>
        /// Gets a message decoder for the specified opcode.
        /// </summary>
        /// <param name="opcode">The opcode of the message.</param>
        /// <returns>A <see cref="IRaidoMessageDecoder"/> for the specified opcode, or <c>null</c> if no decoder is found.</returns>
        public IRaidoMessageDecoder? GetMessageDecoder(int opcode);

        /// <summary>
        /// Gets a message encoder for the specified message type.
        /// </summary>
        /// <param name="messageType">The type of the message.</param>
        /// <returns>A <see cref="IRaidoMessageEncoder"/> for the specified message type, or <c>null</c> if no encoder is found.</returns>
        public IRaidoMessageEncoder? GetMessageEncoder(Type messageType);
    }
}