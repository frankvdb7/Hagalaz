using System.IO;
using System.Text;

namespace Hagalaz.Cache.Extensions
{
    public static class BigEndianMemoryStreamExtensions
    {
        /// <summary>
        /// Gets the remaining.
        /// </summary>
        public static int Remaining(this MemoryStream stream) => (int)(stream.Length - stream.Position);
        
        /// <summary>
        /// Flips the stream.
        /// </summary>
        /// <returns></returns>
        public static void Flip(this MemoryStream stream)
        {
            stream.SetLength(stream.Position);
            stream.Position = 0;
        }
        
        /// <summary>
        /// Reset's the buffer position.
        /// </summary>
        public static void Rewind(this MemoryStream stream) => stream.Position = 0;
        
        /// <summary>
        /// Writers big smart to stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="value">The value.</param>
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
        /// Writes int to stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="value">The value.</param>
        public static void WriteInt(this MemoryStream stream, int value)
        {
            stream.WriteByte((byte)(value >> 24));
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)value);
        }

        /// <summary>
        /// Writes the med int.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="value">The value.</param>
        public static void WriteMedInt(this MemoryStream stream, int value)
        {
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)value);
        }

        /// <summary>
        /// Writes short to stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="value">The value.</param>
        public static void WriteShort(this MemoryStream stream, int value)
        {
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)value);
        }

        /// <summary>
        /// Writes the byte.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="value">The value.</param>
        public static void WriteByte(this MemoryStream stream, int value) => stream.WriteByte((byte)value);

        /// <summary>
        /// Writes a signed byte to the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The value.</param>
        public static void WriteSignedByte(this MemoryStream stream, sbyte value) => stream.WriteByte((byte)value);

        /// <summary>
        /// Writes a smart value to the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The value.</param>
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
        /// Writes bytes to stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="array">Bytes to write.</param>
        public static void WriteBytes(this MemoryStream stream, byte[] array) => stream.WriteBytes(array, 0, array.Length);

        /// <summary>
        /// Writes bytes to stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="array">Bytes to write.</param>
        /// <param name="offset">Offset from which to start reading write array.</param>
        /// <param name="count">Number of bytes to write from offset.</param>
        public static void WriteBytes(this MemoryStream stream, byte[] array, int offset, int count) => stream.Write(array, offset, count);

        /// <summary>
        /// Write a string to the buffer.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="value">The value of the string.</param>
        public static void WriteString(this MemoryStream stream, string value)
        {
            stream.WriteBytes(Encoding.ASCII.GetBytes(value));
            stream.WriteByte(0);
        }
        
        /// <summary>
        /// Read's signed(java) byte from buffer.
        /// </summary>
        /// <returns></returns>
        public static int ReadSignedByte(this MemoryStream stream) => (sbyte)stream.ReadByte();
        
        /// <summary>
        /// Read's unsigned byte from buffer.
        /// </summary>
        /// <returns></returns>
        public static int ReadUnsignedByte(this MemoryStream stream) => stream.ReadSignedByte() & 0xFF;
        
        /// <summary>
        /// Read's unsigned short from buffer.
        /// </summary>
        /// <returns></returns>
        public static int ReadUnsignedShort(this MemoryStream stream) => (((stream.ReadUnsignedByte() & 0xff) << 8) | (stream.ReadUnsignedByte() & 0xff));
        
        /// <summary>
        /// Read's signed short.
        /// </summary>
        /// <returns></returns>
        public static short ReadShort(this MemoryStream stream) => (short)stream.ReadUnsignedShort();
        
        /// <summary>
        /// Read's signed integer.
        /// </summary>
        /// <returns></returns>
        public static int ReadInt(this MemoryStream stream) =>
            ((stream.ReadUnsignedByte() & 0xff) << 24) | ((stream.ReadUnsignedByte() & 0xff) << 16) |
            ((stream.ReadUnsignedByte() & 0xff) << 8) | ((stream.ReadUnsignedByte() & 0xff));
        
        /// <summary>
        /// Reads the long.
        /// </summary>
        /// <returns></returns>
        public static long ReadLong(this MemoryStream stream)
        {
            var l = stream.ReadInt() & 0xffffffffL;
            var l2 = stream.ReadInt() & 0xffffffffL;
            return (l << 32) + l2;
        }
        
        /// <summary>
        /// Reads a string from the buffer.
        /// </summary>
        /// <returns>Returns a string.</returns>
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
        /// Reads the versioned string.
        /// </summary>
        /// <returns></returns>
        public static string ReadVString(this MemoryStream stream)
        {
            if (stream.ReadSignedByte() != 0)
                return string.Empty;
            return stream.ReadString();
        }
        
        /// <summary>
        /// Reads a string from the buffer, possible null.
        /// </summary>
        /// <returns>Returns a string.</returns>
        public static string ReadCheckedString(this MemoryStream stream)
        {
            if (stream.ReadSignedByte() == 0)
                return string.Empty;
            stream.Position -= 1;
            return stream.ReadString();
        }
        
        /// <summary>
        /// Read's med int.
        /// </summary>
        /// <returns></returns>
        public static int ReadMedInt(this MemoryStream stream) =>
            ((stream.ReadUnsignedByte() & 0xff) << 16) + ((stream.ReadUnsignedByte() & 0xff) << 8) +
            (stream.ReadUnsignedByte() & 0xff);
        
        /// <summary>
        /// Read's smart_v1 from buffer.
        /// </summary>
        /// <returns></returns>
        public static int ReadSmart(this MemoryStream stream)
        {
            var first = stream.ReadUnsignedByte();
            stream.Position -= 1; // go back a one.
            if (first < 128)
                return stream.ReadUnsignedByte();
            return stream.ReadUnsignedShort() - 32768;
        }
        
        /// <summary>
        /// Read's big smart.
        /// </summary>
        /// <returns></returns>
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
        /// Read's non byte dependant value.
        /// AKA 'Incrementor'
        /// </summary>
        /// <returns></returns>
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