using Raido.Common.Buffers;
using Raido.Common.Messages;

namespace Raido.Common.Protocol
{
    /// <summary>
    /// Represents a message encoder.
    /// </summary>
    public interface IRaidoMessageEncoder
    {
        /// <summary>
        /// Encodes a message to the specified writer.
        /// </summary>
        /// <param name="message">The message to encode.</param>
        /// <param name="output">The writer to encode the message to.</param>
        public void EncodeMessage(RaidoMessage message, IRaidoMessageBinaryWriter output);
    }

    /// <summary>
    /// Represents a message encoder for a specific message type.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to encode.</typeparam>
    public interface IRaidoMessageEncoder<in TMessage> : IRaidoMessageEncoder where TMessage : RaidoMessage
    {
        void IRaidoMessageEncoder.EncodeMessage(RaidoMessage raidoMessage, IRaidoMessageBinaryWriter output)
        {
            if (raidoMessage is TMessage message)
            {
                EncodeMessage(message, output);
            }
        }
        
        /// <summary>
        /// Encodes a message to the specified writer.
        /// </summary>
        /// <param name="message">The message to encode.</param>
        /// <param name="output">The writer to encode the message to.</param>
        public void EncodeMessage(TMessage message, IRaidoMessageBinaryWriter output);
    }
}