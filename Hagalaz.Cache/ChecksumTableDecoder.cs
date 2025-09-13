using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Hagalaz.Cache.Extensions;
using Hagalaz.Security;

namespace Hagalaz.Cache
{
    public class ChecksumTableDecoder : IChecksumTableDecoder
    {
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
