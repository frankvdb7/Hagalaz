using System;
using System.Text;
using Raido.Common;
using Raido.Common.Buffers;

namespace Raido.Server.Extensions
{
    public static class BufferExtensions
    {
        /// <summary>
        /// Writes a big endian int16 to the buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IByteBufferWriter WriteInt16BigEndian(this IByteBufferWriter buffer, short value)
        {
            buffer.WriteByte((byte)(value >> 8));
            buffer.WriteByte((byte)value);
            return buffer;
        }

        /// <summary>
        /// Writes a big endian int24 to the buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IByteBufferWriter WriteInt24BigEndian(this IByteBufferWriter buffer, int value)
        {
            buffer.WriteByte((byte)(value >> 16));
            buffer.WriteByte((byte)(value >> 8));
            buffer.WriteByte((byte)value);
            return buffer;
        }

        /// <summary>
        /// Writes a big endian int32 to the buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IByteBufferWriter WriteInt32BigEndian(this IByteBufferWriter buffer, int value)
        {
            buffer.WriteByte((byte)(value >> 24));
            buffer.WriteByte((byte)(value >> 16));
            buffer.WriteByte((byte)(value >> 8));
            buffer.WriteByte((byte)value);
            return buffer;
        }

        /// <summary>
        /// Writes a big endian int40 to the buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IByteBufferWriter WriteInt40BigEndian(this IByteBufferWriter buffer, long value)
        {
            buffer.WriteByte((byte)(value >> 32));
            buffer.WriteByte((byte)(value >> 24));
            buffer.WriteByte((byte)(value >> 16));
            buffer.WriteByte((byte)(value >> 8));
            buffer.WriteByte((byte)value);
            return buffer;
        }

        /// <summary>
        /// Writes a big endian int64 to the buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IByteBufferWriter WriteInt64BigEndian(this IByteBufferWriter buffer, long value)
        {
            WriteInt32BigEndian(buffer, (int)(value >> 32));
            WriteInt32BigEndian(buffer, (int)(value & -1L));
            return buffer;
        }
        
        /// <summary>
        /// Writes a string to the buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="value"></param>
        /// <param name="writeStartDelimiter">Whether to write the start delimiter byte.</param>
        public static IByteBufferWriter WriteString(this IByteBufferWriter buffer, string value, bool writeStartDelimiter = false)
        {
            if (writeStartDelimiter)
            {
                buffer.WriteByte(RaidoConstants.StringDelimiter);
            }
            var charSpan = value.AsSpan();
            var byteCount = Encoding.ASCII.GetByteCount(charSpan);

            var byteSpan = buffer.GetSpan(byteCount);
            Encoding.ASCII.GetBytes(charSpan, byteSpan);

            buffer.Advance(byteCount);
            buffer.WriteByte(RaidoConstants.StringDelimiter);
            return buffer;
        }
    }
}