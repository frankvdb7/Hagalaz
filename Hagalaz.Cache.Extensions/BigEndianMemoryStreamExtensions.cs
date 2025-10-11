using System.IO;
using System.Text;

namespace Hagalaz.Cache.Extensions
{
    /// <summary>
    /// Provides extension methods for <see cref="MemoryStream"/> to read and write data
    /// in big-endian format, commonly used in network protocols and legacy file formats.
    /// </summary>
    public static class BigEndianMemoryStreamExtensions
    {
        /// <summary>
        /// Calculates the number of bytes remaining in the stream from the current position to the end.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <returns>The number of remaining bytes.</returns>
        public static int Remaining(this MemoryStream stream) => (int)(stream.Length - stream.Position);
        
        /// <summary>
        /// Prepares the stream for reading after a series of write operations.
        /// It sets the stream's length to the current position and then resets the position to the beginning.
        /// </summary>
        /// <param name="stream">The memory stream to flip.</param>
        public static void Flip(this MemoryStream stream)
        {
            stream.SetLength(stream.Position);
            stream.Position = 0;
        }
        
        /// <summary>
        /// Resets the stream's position to the beginning (index 0).
        /// </summary>
        /// <param name="stream">The memory stream to rewind.</param>
        public static void Rewind(this MemoryStream stream) => stream.Position = 0;
        
        /// <summary>
        /// Writes an integer using a variable-length "big smart" format.
        /// If the value is less than <c>short.MaxValue</c>, it is written as a 2-byte short;
        /// otherwise, it is written as a 4-byte integer.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <param name="value">The integer value to write.</param>
        public static void WriteBigSmart(this MemoryStream stream, int value)
        {
            if (value >= short.MaxValue)
            {
                stream.WriteInt(value - int.MaxValue - 1);
            }
            else
            {
                stream.WriteShort(value >= 0 ? value : short.MaxValue);
            }
        }

        /// <summary>
        /// Writes a 4-byte integer to the stream in big-endian byte order.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <param name="value">The integer value to write.</param>
        public static void WriteInt(this MemoryStream stream, int value)
        {
            stream.WriteByte((byte)(value >> 24));
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)value);
        }

        /// <summary>
        /// Writes a 3-byte integer (medium integer) to the stream in big-endian byte order.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <param name="value">The integer value to write. Only the lower 24 bits are used.</param>
        public static void WriteMedInt(this MemoryStream stream, int value)
        {
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)value);
        }

        /// <summary>
        /// Writes a 2-byte integer (short) to the stream in big-endian byte order.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <param name="value">The integer value to write. Only the lower 16 bits are used.</param>
        public static void WriteShort(this MemoryStream stream, int value)
        {
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)value);
        }

        /// <summary>
        /// Writes a single byte to the stream.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <param name="value">The integer value to write. Only the lower 8 bits are used.</param>
        public static void WriteByte(this MemoryStream stream, int value) => stream.WriteByte((byte)value);

        /// <summary>
        /// Writes a single signed byte to the stream.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <param name="value">The signed byte value to write.</param>
        public static void WriteSignedByte(this MemoryStream stream, sbyte value) => stream.WriteByte((byte)value);

        /// <summary>
        /// Writes an integer using a variable-length "smart" format.
        /// If the value is less than 128, it is written as a single byte;
        /// otherwise, it is written as a 2-byte short with an offset of 32768.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <param name="value">The integer value to write.</param>
        public static void WriteSmart(this MemoryStream stream, int value)
        {
            if (value < 128)
            {
                stream.WriteByte((byte)value);
            }
            else
            {
                stream.WriteShort(value + 32768);
            }
        }

        /// <summary>
        /// Writes an entire byte array to the stream.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <param name="array">The byte array to write.</param>
        public static void WriteBytes(this MemoryStream stream, byte[] array) => stream.WriteBytes(array, 0, array.Length);

        /// <summary>
        /// Writes a segment of a byte array to the stream.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <param name="array">The byte array to write from.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="array"/> at which to begin writing.</param>
        /// <param name="count">The number of bytes to write.</param>
        public static void WriteBytes(this MemoryStream stream, byte[] array, int offset, int count) => stream.Write(array, offset, count);

        /// <summary>
        /// Writes a string to the stream as a sequence of ASCII bytes, terminated by a null byte (0).
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <param name="value">The string to write.</param>
        public static void WriteString(this MemoryStream stream, string value)
        {
            stream.WriteBytes(Encoding.ASCII.GetBytes(value));
            stream.WriteByte(0);
        }
        
        /// <summary>
        /// Reads a single signed byte from the stream.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <returns>The signed byte as an integer.</returns>
        public static int ReadSignedByte(this MemoryStream stream) => (sbyte)stream.ReadByte();
        
        /// <summary>
        /// Reads a single unsigned byte from the stream.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <returns>The unsigned byte as an integer.</returns>
        public static int ReadUnsignedByte(this MemoryStream stream) => stream.ReadSignedByte() & 0xFF;
        
        /// <summary>
        /// Reads a 2-byte unsigned short from the stream in big-endian byte order.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <returns>The unsigned short as an integer.</returns>
        public static int ReadUnsignedShort(this MemoryStream stream) => (((stream.ReadUnsignedByte() & 0xff) << 8) | (stream.ReadUnsignedByte() & 0xff));
        
        /// <summary>
        /// Reads a 2-byte signed short from the stream in big-endian byte order.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <returns>The signed short value.</returns>
        public static short ReadShort(this MemoryStream stream) => (short)stream.ReadUnsignedShort();
        
        /// <summary>
        /// Reads a 4-byte signed integer from the stream in big-endian byte order.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <returns>The signed integer value.</returns>
        public static int ReadInt(this MemoryStream stream) =>
            ((stream.ReadUnsignedByte() & 0xff) << 24) | ((stream.ReadUnsignedByte() & 0xff) << 16) |
            ((stream.ReadUnsignedByte() & 0xff) << 8) | ((stream.ReadUnsignedByte() & 0xff));
        
        /// <summary>
        /// Reads an 8-byte signed long from the stream in big-endian byte order.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <returns>The signed long value.</returns>
        public static long ReadLong(this MemoryStream stream)
        {
            var l = stream.ReadInt() & 0xffffffffL;
            var l2 = stream.ReadInt() & 0xffffffffL;
            return (l << 32) + l2;
        }
        
        /// <summary>
        /// Reads a null-terminated ASCII string from the stream.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <returns>The decoded string.</returns>
        public static string ReadString(this MemoryStream stream)
        {
            var sb = new StringBuilder();
            byte b;

            while ((b = (byte)stream.ReadSignedByte()) != 0)
            {
                sb.Append((char)b);
            }
            return sb.ToString();
        }
        
        /// <summary>
        /// Reads a "versioned" string, which is a standard null-terminated string prefixed by a version byte (which must be 0).
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <returns>The decoded string, or an empty string if the version byte is not 0.</returns>
        public static string ReadVString(this MemoryStream stream)
        {
            if (stream.ReadSignedByte() != 0)
                return string.Empty;
            return stream.ReadString();
        }
        
        /// <summary>
        /// Reads a string that may or may not be present. If the first byte is 0, an empty string is returned.
        /// Otherwise, it reads a standard null-terminated string.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <returns>The decoded string.</returns>
        public static string ReadCheckedString(this MemoryStream stream)
        {
            if (stream.ReadSignedByte() == 0)
                return string.Empty;
            stream.Position -= 1;
            return stream.ReadString();
        }
        
        /// <summary>
        /// Reads a 3-byte medium integer from the stream in big-endian byte order.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <returns>The medium integer value.</returns>
        public static int ReadMedInt(this MemoryStream stream) =>
            ((stream.ReadUnsignedByte() & 0xff) << 16) + ((stream.ReadUnsignedByte() & 0xff) << 8) +
            (stream.ReadUnsignedByte() & 0xff);
        
        /// <summary>
        /// Reads a "smart" encoded integer from the stream. A value less than 128 is read as a single byte;
        /// otherwise, it is read as a 2-byte short with an offset of -32768.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <returns>The decoded integer value.</returns>
        public static int ReadSmart(this MemoryStream stream)
        {
            var first = stream.ReadUnsignedByte();
            stream.Position -= 1; // go back a one.
            if (first < 128)
                return stream.ReadUnsignedByte();
            return stream.ReadUnsignedShort() - 32768;
        }
        
        /// <summary>
        /// Reads a "big smart" encoded integer from the stream. If the first byte is non-negative,
        /// it reads a 2-byte short; otherwise, it reads a 4-byte integer.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <returns>The decoded integer value.</returns>
        public static int ReadBigSmart(this MemoryStream stream)
        {
            var first = stream.ReadSignedByte();
            stream.Position -= 1;
            if (first >= 0)
            {
                return stream.ReadUnsignedShort();
            }
            return stream.ReadInt() & 0x7fffffff;
        }
        
        /// <summary>
        /// Reads a "huge smart" value, which is an integer encoded as a sequence of "smart" values.
        /// This format allows for encoding arbitrarily large numbers by summing successive chunks.
        /// </summary>
        /// <param name="stream">The memory stream.</param>
        /// <returns>The decoded integer value.</returns>
        public static int ReadHugeSmart(this MemoryStream stream)
        {
            var total = 0;
            var value = stream.ReadSmart();
            while (value == 32767)
            {
                value = stream.ReadSmart();
                total += 32767;
            }
            total += value;
            return total;
        }
    }
}