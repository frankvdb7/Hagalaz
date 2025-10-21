using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raido.Common.Buffers
{
    /// <summary>
    /// Represents a byte buffer writer.
    /// </summary>
    public interface IByteBufferWriter : IBufferWriter<byte>
    {
        /// <summary>
        /// Gets the total length of the buffer.
        /// </summary>
        public long Length { get; }

        /// <summary>
        /// Writes a sequence of bytes to the buffer.
        /// </summary>
        /// <param name="span">The sequence of bytes to write.</param>
        /// <returns>The current instance of the <see cref="IByteBufferWriter"/>.</returns>
        public IByteBufferWriter Write(ReadOnlySpan<byte> span);

        /// <summary>
        /// Writes a single byte to the buffer.
        /// </summary>
        /// <param name="value">The byte to write.</param>
        /// <returns>The current instance of the <see cref="IByteBufferWriter"/>.</returns>
        public IByteBufferWriter WriteByte(byte value);
    }
}
