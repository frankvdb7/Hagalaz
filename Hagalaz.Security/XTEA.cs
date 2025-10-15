using System;
using System.Buffers.Binary;

namespace Hagalaz.Security
{
    /// <summary>
    /// Provides methods for XTEA (eXtended Tiny Encryption Algorithm) encryption and decryption.
    /// </summary>
    public static class XTEA
    {
        private const int _blockSize = 8;
        private const uint _delta = 0x9E3779B9;
        private const uint _sum = 0xC6EF3720; // sum on decrypt

        /// <summary>
        /// Encrypts a source buffer into an XTEA-encrypted output buffer.
        /// </summary>
        /// <param name="source">The buffer to encrypt.</param>
        /// <param name="output">The buffer to write the encrypted data to.</param>
        /// <param name="keys">The XTEA keys to use for encryption.</param>
        /// <param name="start">The starting position in the source buffer. Defaults to 0.</param>
        /// <param name="rounds">The number of XTEA rounds. Defaults to 32.</param>
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

        /// <summary>
        /// Encrypts a single 8-byte block of data using the XTEA algorithm.
        /// </summary>
        /// <param name="source">The source buffer containing the block to encrypt.</param>
        /// <param name="output">The destination buffer to write the encrypted block to.</param>
        /// <param name="keys">The XTEA keys to use for encryption.</param>
        /// <param name="start">The starting position of the block in the source buffer.</param>
        /// <param name="rounds">The number of XTEA rounds.</param>
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
        /// Decrypts an XTEA-encrypted source buffer into an output buffer.
        /// </summary>
        /// <param name="source">The buffer containing the encrypted data.</param>
        /// <param name="output">The buffer to write the decrypted data to.</param>
        /// <param name="keys">The XTEA keys to use for decryption.</param>
        /// <param name="start">The starting position in the source buffer. Defaults to 0.</param>
        /// <param name="rounds">The number of XTEA rounds. Defaults to 32.</param>
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

        /// <summary>
        /// Decrypts a single 8-byte block of data using the XTEA algorithm.
        /// </summary>
        /// <param name="source">The source buffer containing the block to decrypt.</param>
        /// <param name="output">The destination buffer to write the decrypted block to.</param>
        /// <param name="keys">The XTEA keys to use for decryption.</param>
        /// <param name="start">The starting position of the block in the source buffer.</param>
        /// <param name="rounds">The number of XTEA rounds.</param>
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
