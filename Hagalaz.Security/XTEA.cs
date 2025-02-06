using System;
using System.Buffers.Binary;

namespace Hagalaz.Security
{
    public static class XTEA
    {
        private const int _blockSize = 8;
        private const uint _delta = 0x9E3779B9;
        private const uint _sum = 0xC6EF3720; // sum on decrypt

        /// <summary>
        /// Decrypt an source to an XTEA output.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="output"></param>
        /// <param name="keys"></param>
        /// <param name="start">Default is 0</param>
        /// <param name="rounds">Default is 32 rounds</param>
        /// <returns></returns>
        public static void Encrypt(ReadOnlySpan<byte> source, Span<byte> output, uint[] keys, int start = 0, int rounds = 32)
        {
            var totalBlockSize = (source.Length - start) / _blockSize; // first off , we calculate how much data we can decrypt
                                                                       // this is simple cause decryption reads 2 ints and then writes those 2 decrypted
                                                                       // so minimum size of the data must be 8 bytes.

            var position = start;
            for (var i = 0; i < totalBlockSize; i++)
            {
                EncryptBlock(source, output, keys, position, rounds);
                position += _blockSize;
            }
        }

        public static void EncryptBlock(ReadOnlySpan<byte> source, Span<byte> output, uint[] keys, int start, int rounds)
        {
            var v0 = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(start, 4));
            var v1 = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(start + 4, 4));
            uint sum = 0;

            for (var i = 0; i != rounds; i++)
            {
                v0 += ((v1 << 4 ^ v1 >> 5) + v1) ^ (sum + keys[sum & 3]);
                sum += _delta;
                v1 += ((v0 << 4 ^ v0 >> 5) + v0) ^ (sum + keys[sum >> 11 & 3]);
            }

            BinaryPrimitives.WriteUInt32BigEndian(output.Slice(start, 4), v0);
            BinaryPrimitives.WriteUInt32BigEndian(output.Slice(start + 4, 4), v1);
        }

        /// <summary>
        /// Decrypt an XTEA source to an output.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="output"></param>
        /// <param name="keys"></param>
        /// <param name="start">Default is 0</param>
        /// <param name="rounds">Default is 32 rounds</param>
        /// <returns></returns>
        public static void Decrypt(ReadOnlySpan<byte> source, Span<byte> output, uint[] keys, int start = 0, int rounds = 32)
        {
            var totalBlockSize = (source.Length - start) / _blockSize; // first off , we calculate how much data we can decrypt
                                                                // this is simple cause decryption reads 2 ints and then writes those 2 decrypted
                                                                // so minimum size of the data must be 8 bytes.
            var position = start;
            for (var i = 0; i < totalBlockSize; i++)
            {
                DecryptBlock(source, output, keys, position, rounds);
                position += _blockSize;
            }
        }

        public static void DecryptBlock(ReadOnlySpan<byte> source, Span<byte> output, uint[] keys, int start, int rounds)
        {
            var v0 = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(start, 4));
            var v1 = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(start + 4, 4));
            var sum = _sum;

            for (var i = 0; i != rounds; i++)
            {
                v1 -= ((v0 << 4 ^ v0 >> 5) + v0) ^ (sum + keys[sum >> 11 & 3]);
                sum -= _delta;
                v0 -= ((v1 << 4 ^ v1 >> 5) + v1) ^ (sum + keys[sum & 3]);
            }

            BinaryPrimitives.WriteUInt32BigEndian(output.Slice(start, 4), v0);
            BinaryPrimitives.WriteUInt32BigEndian(output.Slice(start + 4, 4), v1);
        }
    }
}
