namespace Raido.Common.Protocol
{
    /// <summary>
    /// Represents a bit writer for Raido messages.
    /// </summary>
    public interface IRaidoMessageBitWriter : IRaidoMessageBitWriterEnd
    {
        /// <summary>
        /// Writes a specified number of bits to the message.
        /// </summary>
        /// <param name="bitCount">The number of bits to write.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>The current instance of the <see cref="IRaidoMessageBitWriter"/>.</returns>
        IRaidoMessageBitWriter WriteBits(int bitCount, int value);
    }

    /// <summary>
    /// Represents the beginning of a bit access operation.
    /// </summary>
    public interface IRaidoMessageBitWriterBegin
    {
        /// <summary>
        /// Begins a bit access operation.
        /// </summary>
        /// <returns>A <see cref="IRaidoMessageBitWriter"/> that can be used to write bits to the message.</returns>
        IRaidoMessageBitWriter BeginBitAccess();
    }

    /// <summary>
    /// Represents the end of a bit access operation.
    /// </summary>
    public interface IRaidoMessageBitWriterEnd
    {
        /// <summary>
        /// Ends a bit access operation.
        /// </summary>
        /// <returns>A <see cref="IRaidoMessageBinaryWriter"/> that can be used to write binary data to the message.</returns>
        IRaidoMessageBinaryWriter EndBitAccess();
    }
}
