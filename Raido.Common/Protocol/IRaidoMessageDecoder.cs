using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Raido.Common.Protocol
{
    /// <summary>
    /// Represents a message decoder.
    /// </summary>
    public interface IRaidoMessageDecoder
    {
        /// <summary>
        /// Tries to decode a message from the specified buffer.
        /// </summary>
        /// <param name="input">The buffer containing the message to decode.</param>
        /// <param name="message">When this method returns, contains the decoded message, if the decoding succeeded, or <c>null</c> if the decoding failed.</param>
        /// <returns><c>true</c> if a message was successfully decoded; otherwise, <c>false</c>.</returns>
        bool TryDecodeMessage(in ReadOnlySequence<byte> input, out RaidoMessage? message);
    }
}