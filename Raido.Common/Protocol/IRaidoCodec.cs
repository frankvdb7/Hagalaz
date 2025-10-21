using System.Buffers;

namespace Raido.Common.Protocol
{
    /// <summary>
    /// Represents a message codec.
    /// </summary>
    /// <typeparam name="TProtocol">The type of the protocol.</typeparam>
    public interface IRaidoCodec<TProtocol> where TProtocol : IRaidoProtocol
    {
        /// <summary>
        /// Tries to decode a message.
        /// </summary>
        /// <param name="opcode">The opcode of the message.</param>
        /// <param name="input">The input buffer.</param>
        /// <param name="message">When this method returns, contains the decoded message, if the decoding succeeded, or <c>null</c> if the decoding failed.</param>
        /// <returns><c>true</c> if the message was successfully decoded; otherwise, <c>false</c>.</returns>
        bool TryDecodeMessage(int opcode, in ReadOnlySequence<byte> input, out RaidoMessage? message);

        /// <summary>
        /// Tries to encode a message.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="message">The message to encode.</param>
        /// <param name="output">The output writer.</param>
        /// <returns><c>true</c> if the message was successfully encoded; otherwise, <c>false</c>.</returns>
        bool TryEncodeMessage<TMessage>(TMessage message, IRaidoMessageBinaryWriter output) where TMessage : RaidoMessage;
    }
}