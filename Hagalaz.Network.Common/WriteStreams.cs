using System;
using System.Text;

namespace Hagalaz.Network.Common
{
    /// <summary>
    /// Provides writing functions to work with bytes.
    /// </summary>
    public abstract partial class PacketComposer
    {
        #region Methods
        /// <summary>
        /// Appends a number of bits to the buffer.
        /// </summary>
        /// <param name="numBits">Number of bits to add.</param>
        /// <param name="value">Values for the bits.</param>
        public void AppendBits(int numBits, int value)
        {
            int bytePos = BitPosition >> 3;
            int bitOffset = 8 - (BitPosition & 7);
            BitPosition += numBits;
            Position = (BitPosition + 7) / 8;
            EnsureCapacity(Position);
            for (; numBits > bitOffset; bitOffset = 8)
            {
                _payload[bytePos] &= (byte)~Bitmasks[bitOffset];	 // mask out the desired area
                _payload[bytePos++] |= (byte)((value >> (numBits - bitOffset)) & Bitmasks[bitOffset]);

                numBits -= bitOffset;
            }
            if (numBits == bitOffset)
            {
                _payload[bytePos] &= (byte)~Bitmasks[bitOffset];
                _payload[bytePos] |= (byte)(value & Bitmasks[bitOffset]);
            }
            else
            {
                _payload[bytePos] &= (byte)~(Bitmasks[numBits] << (bitOffset - numBits));
                _payload[bytePos] |= (byte)((value & Bitmasks[numBits]) << (bitOffset - numBits));
            }
        }

        /// <summary>
        /// Appends a byte to the buffer and advanced one index.
        /// </summary>
        /// <param name="value">The value of the integer.</param>
        /// <param name="checkCapacity">Whether to check for the capacity.</param>
        public void AppendByte(byte value, bool checkCapacity)
        {
            if (checkCapacity)
                EnsureCapacity(Position + 1);
            _payload[Position++] = value;
        }

        /// <summary>
        /// Appends a byte to the buffer.
        /// </summary>
        /// <param name="value">The value of the integer.</param>
        /// <returns>Returns this instance.</returns>
        public void AppendByte(byte value)
        {
            AppendByte(value, true);
        }

        /// <summary>
        /// Appends 3byte datatype to buffer.
        /// </summary>
        /// <param name="value"></param>
        public void AppendMedInt(int value)
        {
            EnsureCapacity(Position + 3);
            AppendByte((byte)(value >> 16), false);
            AppendByte((byte)(value >> 8), false);
            AppendByte((byte)value, false);
        }

        /// <summary>
        /// Appends 3byte datatype to buffer.
        /// </summary>
        /// <param name="value"></param>
        public void AppendMedIntSecondary(int value)
        {
            EnsureCapacity(Position + 3);
            AppendByte((byte)(value >> 8), false);
            AppendByte((byte)(value >> 16), false);
            AppendByte((byte)value, false);
        }

        /// <summary>
        /// Appends an A type byte to the buffer.
        /// </summary>
        /// <param name="value">The value of the integer.</param>
        /// <returns>Returns this instance.</returns>
        public void AppendByteA(byte value)
        {
            AppendByte((byte)(value + 128), true);
        }

        /// <summary>
        /// Appends an C type byte to the buffer.
        /// </summary>
        /// <param name="value">The value of the integer.</param>
        /// <returns>Returns this instance.</returns>
        public void AppendByteC(byte value)
        {
            AppendByte((byte)-value);
        }

        /// <summary>
        /// Appends an S type byte to the buffer.
        /// </summary>
        /// <param name="value">The value of the integer.</param>
        /// <returns>Returns this instance.</returns>
        public void AppendByteS(byte value)
        {
            AppendByte((byte)(128 - value));
            //AppendByte((byte)(-value - 128));
        }

        /// <summary>
        /// Appends an S type byte to the buffer.
        /// </summary>
        /// <param name="value">The value of the integer.</param>
        /// <returns>Returns this instance.</returns>
        public void AppendNegativeByteS(byte value)
        {
            AppendByte((byte)(128 - (-value)));
        }

        /// <summary>
        /// Appends a series of bytes to the buffer.
        /// </summary>
        /// <param name="data">The byte array to append.</param>
        /// <param name="offset">The startpoint in the buffer to start copying.</param>
        /// <param name="length">How long data to copy from the byte array.</param>
        /// <returns>Returns this instance.</returns>
        public void AppendBytes(byte[] data, int offset, int length)
        {
            int newPos = Position + length;
            EnsureCapacity(newPos);
            Buffer.BlockCopy(data, offset, _payload, Position, length);
            Position = newPos;
        }

        /// <summary>
        /// Appends a series of bytes to the buffer.
        /// </summary>
        /// <param name="data">The byte array to append.</param>
        /// <returns>returns this instance.</returns>
        public void Append(byte[] data)
        {
            AppendBytes(data, 0, data.Length);
        }

        /// <summary>
        /// Appends a series of bytes to the buffer.
        /// </summary>
        /// <param name="data">The byte array to append.</param>
        /// <returns>returns this instance.</returns>
        public void AppendBytes(byte[] data)
        {
            AppendBytes(data, 0, data.Length);
        }

        /// <summary>
        /// Appends a short to the buffer.
        /// </summary>
        /// <param name="value">The value of the integer.</param>
        /// <returns>Returns this instance.</returns>
        public void AppendShort(short value)
        {
            EnsureCapacity(Position + 2);
            AppendByte((byte)(value >> 8), false);
            AppendByte((byte)value, false);
        }

        /// <summary>
        /// Appends a little endian short to the buffer.
        /// </summary>
        /// <param name="value">The value of the integer.</param>
        /// <returns>Returns this instance.</returns>
        public void AppendLeShort(short value)
        {
            EnsureCapacity(Position + 2);
            AppendByte((byte)value, false);
            AppendByte((byte)(value >> 8), false);
        }

        /// <summary>
        /// Appends an A type short to the buffer.
        /// </summary>
        /// <param name="value">The value of the integer.</param>
        /// <returns>Returns this instance.</returns>
        public void AppendShortA(short value)
        {
            EnsureCapacity(Position + 2);
            AppendByte((byte)(value >> 8), false);
            AppendByte((byte)(value + 128), false);
        }

        /// <summary>
        /// Appends a little endian A type short the buffer.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Returns this instance.</returns>
        public void AppendLeShortA(short value)
        {
            EnsureCapacity(Position + 2);
            AppendByte((byte)(value + 128), false);
            AppendByte((byte)(value >> 8), false);
        }

        /// <summary>
        /// Appends an int to the buffer.
        /// </summary>
        /// <param name="value">The value of the integer.</param>
        /// <returns>Returns this instance.</returns>
        public void AppendInt(int value)
        {
            EnsureCapacity(Position + 4);
            AppendByte((byte)(value >> 24), false);
            AppendByte((byte)(value >> 16), false);
            AppendByte((byte)(value >> 8), false);
            AppendByte((byte)value, false);
        }

        /// <summary>
        /// Appends an secondary type int to the buffer.
        /// </summary>
        /// <param name="value">The value of the integer.</param>
        /// <returns>Returns this instance.</returns>
        public void AppendIntSecondary(int value)
        {
            EnsureCapacity(Position + 4);
            AppendByte((byte)(value >> 8), false);
            AppendByte((byte)value, false);
            AppendByte((byte)(value >> 24), false);
            AppendByte((byte)(value >> 16), false);
        }

        /// <summary>
        /// Appends an tertiary type int to the buffer.
        /// </summary>
        /// <param name="value">The value of the integer.</param>
        /// <returns>Returns this instance.</returns>
        public void AppendIntTertiary(int value)
        {
            EnsureCapacity(Position + 4);
            AppendByte((byte)(value >> 16), false);
            AppendByte((byte)(value >> 24), false);
            AppendByte((byte)value, false);
            AppendByte((byte)(value >> 8), false);
        }

        /// <summary>
        /// Appends a little endian int to the buffer.
        /// </summary>
        /// <param name="value">The value of the integer.</param>
        /// <returns>Returns this instnace.</returns>
        public void AppendLeInt(int value)
        {
            EnsureCapacity(Position + 4);
            AppendByte((byte)value, false);
            AppendByte((byte)(value >> 8), false);
            AppendByte((byte)(value >> 16), false);
            AppendByte((byte)(value >> 24), false);
        }

        /// <summary>
        /// Appends a long to the buffer.
        /// </summary>
        /// <param name="value">The value of the integer.</param>
        /// <returns>Returns this instance.</returns>
        public void AppendLong(long value)
        {
            AppendInt((int)(value >> 32));
            AppendInt((int)(value & -1L));
        }

        /// <summary>
        /// Appends a long to the buffer.
        /// </summary>
        /// <param name="value">The value of the integer.</param>
        /// <returns>Returns this instance.</returns>
        public void AppendLeLong(long value)
        {
            AppendLeInt((int)(value & -1L));
            AppendLeInt((int)(value >> 32));
        }

        /// <summary>
        /// Appends a string to the buffer.
        /// </summary>
        /// <param name="value">The value of the string.</param>
        /// <returns>Returns this instance.</returns>
        public void AppendString(string value)
        {
            EnsureCapacity(Position + value.Length + 1);
            AppendBytes(Encoding.ASCII.GetBytes(value));
            AppendByte(0);
        }

        /// <summary>
        /// Appends a secondary type string to the buffer.
        /// </summary>
        /// <param name="value">The value of the string.</param>
        /// <returns>Returns this instance.</returns>
        public void AppendStringSecondary(string value)
        {
            AppendByte(0);
            AppendString(value);
        }

        /// <summary>
        /// Appends byte or short depending on its value.
        /// </summary>
        /// <param name="value"></param>
        public void AppendSmart(short value)
        {
            if (value >= 128)
            {
                AppendShort((short)(value + 32768));
            }
            else
            {
                AppendByte((byte)value);
            }
        }

        /// <summary>
        /// Appends int or short depending on its value.
        /// </summary>
        /// <param name="value"></param>
        public void AppendBigSmart(int value)
        {
            if (value >= short.MaxValue)
            {
                AppendInt(value - int.MaxValue - 1);
            }
            else
            {
                AppendShort(value >= 0 ? (short)value : (short)32767);
            }
        }

        /// <summary>
        /// Appends 5 byte datatype to the buffer.
        /// </summary>
        /// <param name="value"></param>
        public void Append5Byte(long value)
        {
            AppendByte((byte)(value >> 32));
            AppendInt((int)(value & 0xffffffff));
        }

        /// <summary>
        /// Appends Packet GJ String to the buffer.
        /// </summary>
        /// <param name="stringtoput"></param>
        public void AppendJString(string stringtoput)
        {
            AppendByte(0);
            byte[] packed = new byte[256];
            int length = PackJString(0, packed, stringtoput);
            for (int i = 0; i < length; i++)
            {
                AppendByte(packed[i]);
            }
            AppendByte(0);
        }
        
        /// <summary>
        /// Packs GJString
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="buffer">The buffer.</param>
        /// <param name="strtopack">The strtopack.</param>
        /// <returns>System.Int32.</returns>
        private static int PackJString(int position, byte[] buffer, string strtopack)
        {
            int length = strtopack.Length;
            int offset = position;
            for (int i = 0; length > i; i++)
            {
                int character = strtopack[i];
                if (character > 127)
                {
                    if (character > 2047)
                    {
                        buffer[offset++] = (byte)((character | 919275) >> 12);
                        buffer[offset++] = (byte)(128 | ((character >> 6) & 63));
                        buffer[offset++] = (byte)(128 | (character & 63));
                    }
                    else
                    {
                        buffer[offset++] = (byte)((character | 12309) >> 6);
                        buffer[offset++] = (byte)(128 | (character & 63));
                    }
                }
                else
                    buffer[offset++] = (byte)character;
            }
            return offset - position;
        }
        #endregion Methods
    }
}
