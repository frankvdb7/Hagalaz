using Raido.Common.Buffers;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Extensions
{
    public static class BufferExtensions
    {
        /// <summary>
        /// Writes a substracted byte.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IByteBufferWriter WriteByteS(this IByteBufferWriter buffer, byte value) => buffer.WriteByte((byte)(128 - value));

        /// <summary>\
        /// 5
        /// Writes a additive byte.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IByteBufferWriter WriteByteA(this IByteBufferWriter buffer, byte value) => buffer.WriteByte((byte)(128 + value));

        /// <summary>
        /// Writes a negative byte.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IByteBufferWriter WriteByteC(this IByteBufferWriter buffer, byte value) => buffer.WriteByte((byte)-value);

        /// <summary>
        /// Writes an addition big endian int16.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IByteBufferWriter WriteInt16BigEndianA(this IByteBufferWriter buffer, short value) => buffer
            .WriteByte((byte)(value >> 8))
            .WriteByte((byte)(value + 128));

        /// <summary>
        /// Writes an int16 or byte to the buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IByteBufferWriter WriteInt16BigEndianSmart(this IByteBufferWriter buffer, short value) =>
            value <= sbyte.MaxValue ?
            buffer.WriteByte((byte)value) :
            buffer.WriteInt16BigEndian((short)(value + 32768));

        /// <summary>
        /// Writes a little endian int16
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IByteBufferWriter WriteInt16LittleEndian(this IByteBufferWriter buffer, short value) => buffer
            .WriteByte((byte)value)
            .WriteByte((byte)(value >> 8));

        /// <summary>
        /// Writes an addition little endian int16
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IByteBufferWriter WriteInt16LittleEndianA(this IByteBufferWriter buffer, short value) => buffer
            .WriteByte((byte)(value + 128))
            .WriteByte((byte)(value >> 8));

        /// <summary>
        /// Writes a little endian int32.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IByteBufferWriter WriteInt32LittleEndian(this IByteBufferWriter buffer, int value) => buffer
            .WriteByte((byte)value)
            .WriteByte((byte)(value >> 8))
            .WriteByte((byte)(value >> 16))
            .WriteByte((byte)(value >> 24));

        /// <summary>
        /// Writes a middle endian int32.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IByteBufferWriter WriteInt32MiddleEndian(this IByteBufferWriter buffer, int value) => buffer
                .WriteByte((byte)(value >> 8))
                .WriteByte((byte)value)
                .WriteByte((byte)(value >> 24))
                .WriteByte((byte)(value >> 16));

        /// <summary>
        /// Writes a mixed endian int32
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IByteBufferWriter WriteInt32MixedEndian(this IByteBufferWriter buffer, int value) => buffer
            .WriteByte((byte)(value >> 16))
            .WriteByte((byte)(value >> 24))
            .WriteByte((byte)value)
            .WriteByte((byte)(value >> 8));

        /// <summary>
        /// Writes an int32 or int16 to the buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IByteBufferWriter WriteInt32BigEndianSmart(this IByteBufferWriter buffer, int value) =>
            value >= short.MaxValue ?
            buffer.WriteInt32BigEndian(value - int.MaxValue - 1) :
            buffer.WriteInt16BigEndian(value >= 0 ? (short)value : (short)32767);
    }
}
