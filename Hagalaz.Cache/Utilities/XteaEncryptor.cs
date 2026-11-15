using System;

namespace Hagalaz.Cache.Utilities
{
    /// <summary>
    /// Encrypts cache container bytes with XTEA, the exact inverse of <see cref="XteaDecryptor"/>.
    /// </summary>
    public static class XteaEncryptor
    {
        private const int Delta = -1640531527;

        /// <summary>
        /// Encrypts, in place, every whole 8-byte block of <paramref name="data"/> from <paramref name="offset"/>; a shorter tail stays plain, as when decrypting.
        /// </summary>
        /// <param name="keys">The four XTEA keys.</param>
        /// <param name="data">The bytes to encrypt.</param>
        /// <param name="offset">Where encryption starts.</param>
        /// <param name="length">The end of the encrypted range, normally the buffer length.</param>
        /// <returns><paramref name="data"/>.</returns>
        public static byte[] EncryptXtea(int[] keys, byte[] data, int offset, int length)
        {
            ArgumentNullException.ThrowIfNull(keys);
            ArgumentNullException.ThrowIfNull(data);
            if (keys.Length != 4)
            {
                throw new ArgumentException("Exactly four XTEA keys are required.", nameof(keys));
            }

            var blocks = (length - offset) / 8;
            for (var i = 0; i < blocks; i++)
            {
                var position = offset + i * 8;
                var v0 = ReadInt(data, position);
                var v1 = ReadInt(data, position + 4);
                var sum = 0;
                for (var round = 0; round < 32; round++)
                {
                    v0 += (((v1 << 4) ^ (int)((uint)v1 >> 5)) + v1) ^ (sum + keys[sum & 3]);
                    sum += Delta;
                    v1 += (((v0 << 4) ^ (int)((uint)v0 >> 5)) + v0) ^ (sum + keys[(int)((uint)sum >> 11) & 3]);
                }
                WriteInt(data, position, v0);
                WriteInt(data, position + 4, v1);
            }
            return data;
        }

        private static int ReadInt(byte[] data, int position) =>
            (data[position] << 24) | (data[position + 1] << 16) | (data[position + 2] << 8) | data[position + 3];

        private static void WriteInt(byte[] data, int position, int value)
        {
            data[position] = (byte)(value >> 24);
            data[position + 1] = (byte)(value >> 16);
            data[position + 2] = (byte)(value >> 8);
            data[position + 3] = (byte)value;
        }
    }
}
