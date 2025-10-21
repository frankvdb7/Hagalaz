using System.Buffers;

namespace Raido.Common.Protocol
{
    /// <summary>
    /// Represents a message writer.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    public interface IRaidoMessageWriter<in TMessage>
    {
        /// <summary>
        /// Writes a message to the specified buffer.
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <param name="output">The buffer to write the message to.</param>
        void WriteMessage(TMessage message, IBufferWriter<byte> output);
    }
}