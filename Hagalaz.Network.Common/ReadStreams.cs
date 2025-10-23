using System;
using System.Text;
using System.Threading.Tasks;

namespace Hagalaz.Network.Common
{
    /// <summary>
    /// Represents a packet. Allows multiple ways to read data.
    /// </summary>
    public partial class Packet
    {
        #region Methods
        /// <summary>
        /// Reads the next byte in the packet without removing it from the payload.
        /// </summary>
        /// <returns>Returns a 8-bit integer.</returns>
        public byte Peek()
        {
            byte b = (byte)BaseBuffer.ReadByte();
            BaseBuffer.Position--;
            return b;
        }

        /// <summary>
        /// Reads an A type single byte from the buffer and advances one position.
        /// </summary>
        /// <returns>Returns a 8-bit unsigned integer.</returns>
        public byte ReadByteA() => (byte)(BaseBuffer.ReadByte() - 128);

        /// <summary>
        /// Reads a signed byte from the buffer.
        /// </summary>
        /// <returns>Returns a 8-bit signed integer.</returns>
        public sbyte ReadSignedByte() => (sbyte)BaseBuffer.ReadByte();

        /// <summary>
        /// Reads a single byte from the buffer and advances one position.
        /// </summary>
        /// <returns>Returns a 8-bit unsigned integer.</returns>
        public byte ReadByte() => (byte)BaseBuffer.ReadByte();

        /// <summary>
        /// Reads a C type byte from the buffer.
        /// </summary>
        /// <returns>Returns a 8-bit unsigned integer.</returns>
        public byte ReadByteC() => (byte)-BaseBuffer.ReadByte();

        /// <summary>
        /// Reads a S type byte from the buffer.
        /// </summary>
        /// <returns>Returns a 8-bit unsigned integer.</returns>
        public byte ReadByteS() => (byte)(128 - BaseBuffer.ReadByte());

        /// <summary>
        /// Reads the buffer async and fills up the whole specified array.
        /// </summary>
        /// <param name="array">The array to read values to.</param>
        public Task ReadAsync(byte[] array) => BaseBuffer.ReadAsync(array, 0, array.Length);

        /// <summary>
        /// Reads the buffer and fills up the whole specified array.
        /// </summary>
        /// <param name="array">The array to read values to.</param>
        public void Read(byte[] array) => Read(array, 0, array.Length);

        /// <summary>
        /// Reads an array of bytes to the specified array.
        /// </summary>
        /// <param name="array">The array to read to.</param>
        /// <param name="offset">The start point to start reading.</param>
        /// <param name="length">The length to read for.</param>
        /// <exception cref="InvalidOperationException">Data buffer exhaust.</exception>
        public void Read(byte[] array, int offset, int length)
        {
            if (RemainingAmount < length)
                throw new InvalidOperationException("Data buffer exhaust.");

            BaseBuffer.Read(array, offset, length);
        }

        /// <summary>
        /// Reads a short from the buffer.
        /// </summary>
        /// <returns>Returns a 16-bit integer.</returns>
        public short ReadShort() => (short)(((BaseBuffer.ReadByte() & 0xff) << 8) |
    (BaseBuffer.ReadByte() & 0xff));

        /// <summary>
        /// Reads a type A short from the buffer.
        /// </summary>
        /// <returns>Returns a 16-bit integer.</returns>
        public short ReadShortA() => (short)(((BaseBuffer.ReadByte() & 0xFF) << 8)
    + (BaseBuffer.ReadByte() - 128 & 0xFF));

        /// <summary>
        /// Reads a little endian short from the buffer.
        /// </summary>
        /// <returns>Returns a 16-bit integer.</returns>
        public short ReadLeShort() => (short)(((BaseBuffer.ReadByte() & 0xff)) +
    ((BaseBuffer.ReadByte() & 0xff) << 8));

        /// <summary>
        /// Reads a little endian type A short from the buffer.
        /// </summary>
        /// <returns>Returns a 16-bit integer.</returns>
        public short ReadLeShortA() => (short)(((BaseBuffer.ReadByte() - 128 & 0xff)) +
    ((BaseBuffer.ReadByte() & 0xff) << 8));

        /// <summary>
        /// Reads a int from the buffer.
        /// </summary>
        /// <returns>Returns a 32-bit integer.</returns>
        public int ReadInt() => ((BaseBuffer.ReadByte() & 0xff) << 24)
    | ((BaseBuffer.ReadByte() & 0xff) << 16)
    | ((BaseBuffer.ReadByte() & 0xff) << 8)
    | (BaseBuffer.ReadByte() & 0xff);

        /// <summary>
        /// Reads 24 bits int. ( 3 bytes )
        /// </summary>
        /// <returns>Returns a 32-bit integer.</returns>
        public int ReadMedInt() => ((BaseBuffer.ReadByte() & 0xff) << 16)
    + ((BaseBuffer.ReadByte() & 0xff) << 8)
    + (BaseBuffer.ReadByte() & 0xff);

        /// <summary>
        /// Reads 24 bits int. ( 3 bytes )
        /// </summary>
        /// <returns>Returns a 32-bit integer.</returns>
        public int ReadMedIntSecondary() => ((BaseBuffer.ReadByte() & 0xff) << 8)
    + ((BaseBuffer.ReadByte() & 0xff) << 16)
    + (BaseBuffer.ReadByte() & 0xff);

        /// <summary>
        /// Reads a int from the buffer.
        /// </summary>
        /// <returns>Returns a 32-bit integer.</returns>
        public int ReadIntSecondary() => ((BaseBuffer.ReadByte() & 0xff) << 8)
    | ((BaseBuffer.ReadByte() & 0xff))
    | ((BaseBuffer.ReadByte() & 0xff) << 24)
    | ((BaseBuffer.ReadByte() & 0xff) << 16);

        /// <summary>
        /// Reads a int from the buffer.
        /// </summary>
        /// <returns>Returns a 32-bit integer.</returns>
        public int ReadIntTertiary() => ((BaseBuffer.ReadByte() & 0xff) << 16)
    | ((BaseBuffer.ReadByte() & 0xff) << 24)
    | ((BaseBuffer.ReadByte() & 0xff))
    | ((BaseBuffer.ReadByte() & 0xff) << 8);

        /// <summary>
        /// Reads a little endian from the buffer
        /// </summary>
        /// <returns>Returns a 32-bit integer.</returns>
        public int ReadLeInt() => (BaseBuffer.ReadByte() & 0xff)
    | ((BaseBuffer.ReadByte() & 0xff) << 8)
    | ((BaseBuffer.ReadByte() & 0xff) << 16)
    | ((BaseBuffer.ReadByte() & 0xff) << 24);

        /// <summary>
        /// Reads a "smart" value, which is either a byte or a short, depending on the first byte.
        /// </summary>
        /// <returns>Returns either a byte or a short, cast to an integer.</returns>
        public int ReadSmart()
        {
            if (Peek() < 128)
                return ReadByte();
            return ReadShort();
        }

        /// <summary>
        /// Reads a long from the buffer.
        /// </summary>
        /// <returns>Returns a 64-bit integer.</returns>
        public long ReadLong()
        {
            long l = ReadInt() & 0xffffffffL;
            long l2 = ReadInt() & 0xffffffffL;
            return (l << 32) + l2;
        }

        /// <summary>
        /// Reads a string from the buffer.
        /// </summary>
        /// <returns>Returns a string.</returns>
        public string ReadString()
        {
            var sb = new StringBuilder();

            while (true)
            {
                int i = BaseBuffer.ReadByte();
                if (i == -1)
                    throw new InvalidOperationException("Data buffer exhaust.");
                if (i == 0)
                    break;

                sb.Append((char)i);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Reads a versioned string, which must be prefixed with a 0 byte.
        /// </summary>
        /// <returns>The string read from the buffer.</returns>
        /// <exception cref="Exception">Thrown if the string is not prefixed with a 0 byte.</exception>
        public string ReadVString()
        {
            if (ReadByte() != 0)
                throw new Exception("GJSTR2 - bad magic number");
            return ReadString();
        }

        /// <summary>
        /// Reads a string from the buffer.
        /// </summary>
        /// <returns>Returns a string.</returns>
        public string ReadRsString()
        {
            StringBuilder sb = new StringBuilder();

            while (true)
            {
                int i = BaseBuffer.ReadByte();
                if (i == -1)
                    throw new InvalidOperationException("Data buffer exhaust.");
                if (i == 10)
                    break;

                sb.Append((char)i);
            }
            return sb.ToString();
        }
        #endregion Methods
    }
}
