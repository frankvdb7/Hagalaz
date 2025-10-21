using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Raido.Common.Protocol
{
    /// <summary>
    /// Represents a message reader.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    public interface IRaidoMessageReader<TMessage>
    {
        /// <summary>
        /// Tries to parse a message from the specified buffer.
        /// </summary>
        /// <param name="input">The buffer containing the message to parse.</param>
        /// <param name="consumed">When this method returns, contains the position in the buffer that was consumed.</param>
        /// <param name="examined">When this method returns, contains the position in the buffer that was examined.</param>
        /// <param name="message">When this method returns, contains the parsed message, if the parsing succeeded, or <c>null</c> if the parsing failed.</param>
        /// <returns><c>true</c> if a message was successfully parsed; otherwise, <c>false</c>.</returns>
        bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out TMessage? message);
    }
}