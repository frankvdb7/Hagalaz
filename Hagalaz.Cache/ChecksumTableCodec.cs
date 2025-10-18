using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Hagalaz.Cache.Extensions;
using Hagalaz.Security;

namespace Hagalaz.Cache
{
    /// <summary>
    /// Handles the encoding and decoding of a <see cref="ChecksumTable"/>.
    /// The checksum table is a critical part of the cache's update mechanism, often referred to as "update keys".
    /// </summary>
    public class ChecksumTableCodec : IChecksumTableCodec
    {
        /// <summary>
        /// Encodes a <see cref="ChecksumTable"/> into a memory stream.
        /// </summary>
        /// <param name="table">The checksum table to encode.</param>
        /// <returns>A memory stream containing the encoded checksum table.</returns>
        public MemoryStream Encode(ChecksumTable table) => Encode(table, false);

        /// <summary>
        /// Encodes a <see cref="ChecksumTable"/> into a memory stream, with an option to include whirlpool digests.
        /// </summary>
        /// <param name="table">The checksum table to encode.</param>
        /// <param name="whirlpool">A flag indicating whether to include whirlpool digests for each entry.</param>
        /// <returns>A memory stream containing the encoded checksum table.</returns>
        public MemoryStream Encode(ChecksumTable table, bool whirlpool) => Encode(table, whirlpool, BigInteger.MinusOne, BigInteger.MinusOne);

        /// <summary>
        /// Encodes a <see cref="ChecksumTable"/> into a memory stream, with options for whirlpool digests and RSA encryption.
        /// </summary>
        /// <param name="table">The checksum table to encode.</param>
        /// <param name="whirlpool">A flag indicating whether to include whirlpool digests.</param>
        /// <param name="modulus">The RSA modulus for encryption.</param>
        /// <param name="privateKey">The RSA private key for encryption.</param>
        /// <returns>A memory stream containing the encoded and potentially encrypted checksum table.</returns>
        public MemoryStream Encode(ChecksumTable table, bool whirlpool, BigInteger modulus, BigInteger privateKey)
        {
            var buffer = new MemoryStream();

            /* as the new whirlpool format is more complicated we must write the number of entries */
            if (whirlpool)
                buffer.WriteByte(table.Count);

            /* encode the individual entries */
            foreach (var entry in table)
            {
                buffer.WriteInt(entry.Crc32);
                buffer.WriteInt(entry.Version);
                if (whirlpool)
                    buffer.WriteBytes(entry.Digest);
            }

            /* compute (and encrypt) the digest of the whole table */
            if (whirlpool)
            {
                byte[] data = buffer.ToArray();
                using (var rsa = new MemoryStream(65))
                {
                    rsa.WriteByte(10);
                    rsa.WriteBytes(Whirlpool.GenerateDigest(data, 0, data.Length));

                    data = rsa.ToArray();
                }

                if (modulus != BigInteger.MinusOne && privateKey != BigInteger.MinusOne)
                {
                    var biginteger = new BigInteger(data.Reverse().ToArray()); // big endian to little endian (java)
                    var biginteger2 = BigInteger.ModPow(biginteger, privateKey, modulus);
                    data = biginteger2.ToByteArray().Reverse().ToArray(); // big endian to little endian (java)
                }

                buffer.WriteBytes(data);
            }

            buffer.Flip();
            return buffer;
        }

        public ChecksumTable Decode(MemoryStream stream) => Decode(stream, false);

        public ChecksumTable Decode(MemoryStream stream, bool whirlpool) => Decode(stream, whirlpool, BigInteger.MinusOne, BigInteger.MinusOne);

        public ChecksumTable Decode(MemoryStream stream, bool whirlpool, BigInteger modulus, BigInteger publicKey)
        {
            /* find out how many entries there are and allocate a new table */
            var table = new ChecksumTable(whirlpool ? (stream.ReadSignedByte() & 0xFF) : ((int)stream.Length / 8));

            /* calculate the whirlpool digest we expect to have at the end */
            byte[]? masterDigest = null;
            if (whirlpool)
            {
                byte[] temp = new byte[table.Count * 72 + 1];
                stream.Position = 0;
                stream.Read(temp, 0, temp.Length);
                masterDigest = Whirlpool.GenerateDigest(temp, 0, temp.Length);
            }

            /* read the entries */
            stream.Position = whirlpool ? 1 : 0;
            for (var i = 0; i < table.Count; i++)
            {
                int crc = stream.ReadInt();
                int version = stream.ReadInt();
                byte[] digest = new byte[64];
                if (whirlpool)
                    stream.Read(digest, 0, digest.Length);
                table.SetEntry(i, new ChecksumTableEntry(crc, version, digest));
            }

            /* read the trailing digest and check if it matches up */
            if (!whirlpool)
            {
                return table;
            }

            byte[] bytes = new byte[stream.Remaining()];
            stream.Read(bytes, 0, bytes.Length);

            if (modulus != BigInteger.MinusOne && publicKey != BigInteger.MinusOne)
            {
                var biginteger = new BigInteger(bytes.Reverse().ToArray()); // java big endian array
                var biginteger2 = BigInteger.ModPow(biginteger, publicKey, modulus);
                bytes = biginteger2.ToByteArray().Reverse().ToArray(); // java big endian array
            }

            if (bytes.Length != 65)
                throw new IOException("Decrypted data is not 65 bytes long.");

            for (var i = 0; i < 64; i++)
            {
                if (bytes[i + 1] != masterDigest![i])
                    throw new IOException("Whirlpool digest mismatch.");
            }

            return table;
        }
    }
}
