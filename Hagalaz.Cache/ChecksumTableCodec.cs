using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Hagalaz.Cache.Extensions;
using Hagalaz.Security;

namespace Hagalaz.Cache
{
    public class ChecksumTableCodec : IChecksumTableCodec
    {
        public ChecksumTable Decode(Stream stream, bool whirlpool = false, BigInteger? modulus = null, BigInteger? exponent = null)
        {
            if (!whirlpool)
            {
                var table = new ChecksumTable((int)stream.Length / 8);
                for (var i = 0; i < table.Count; i++)
                {
                    int crc = stream.ReadInt();
                    int version = stream.ReadInt();
                    table.SetEntry(i, new ChecksumTableEntry(crc, version, null));
                }
                return table;
            }

            int entryCount = stream.ReadByte();
            var whirlpoolTable = new ChecksumTable(entryCount);

            using var tableDataStream = new MemoryStream();
            tableDataStream.WriteByte((byte)entryCount);

            var tempBuffer = new byte[72]; // 4+4+64
            for (var i = 0; i < entryCount; i++)
            {
                stream.Read(tempBuffer, 0, tempBuffer.Length);
                tableDataStream.Write(tempBuffer, 0, tempBuffer.Length);

                using var entryReader = new MemoryStream(tempBuffer);
                int crc = entryReader.ReadInt();
                int version = entryReader.ReadInt();
                byte[] digest = new byte[64];
                entryReader.Read(digest, 0, 64);

                whirlpoolTable.SetEntry(i, new ChecksumTableEntry(crc, version, digest));
            }

            byte[] expectedDigest = Whirlpool.GenerateDigest(tableDataStream.ToArray(), 0, (int)tableDataStream.Length);

            if (modulus.HasValue && exponent.HasValue)
            {
                int rsaBlockSize = (modulus.Value.GetBitLength() + 7) / 8;
                var encryptedBytes = new byte[rsaBlockSize];
                stream.Read(encryptedBytes, 0, encryptedBytes.Length);

                var encryptedBigInt = new BigInteger(encryptedBytes, true, true);
                var decryptedBigInt = BigInteger.ModPow(encryptedBigInt, exponent.Value, modulus.Value);
                var decryptedBytes = decryptedBigInt.ToByteArray(true, true);

                if (decryptedBytes[0] != 10)
                {
                    throw new IOException("Invalid RSA padding. Expected 10, got " + decryptedBytes[0]);
                }

                var actualDigest = new byte[64];
                Array.Copy(decryptedBytes, 1, actualDigest, 0, 64);

                if (!expectedDigest.SequenceEqual(actualDigest))
                {
                    throw new IOException("Whirlpool digest mismatch.");
                }
            }

            return whirlpoolTable;
        }

        public MemoryStream Encode(ChecksumTable table, bool whirlpool = false, BigInteger? modulus = null, BigInteger? exponent = null)
        {
            var buffer = new MemoryStream();

            if (whirlpool)
                buffer.WriteByte((byte)table.Count);

            foreach (var entry in table.Entries)
            {
                buffer.WriteInt(entry.Crc32);
                buffer.WriteInt(entry.Version);
                if (whirlpool)
                    buffer.WriteBytes(entry.Digest);
            }

            if (whirlpool)
            {
                byte[] data = buffer.ToArray();
                var whirlpoolDigest = Whirlpool.GenerateDigest(data, 0, data.Length);

                var rsaBlock = new byte[65];
                rsaBlock[0] = 10; // Padding
                Buffer.BlockCopy(whirlpoolDigest, 0, rsaBlock, 1, 64);

                if (modulus.HasValue && exponent.HasValue)
                {
                    var rsaBigInt = new BigInteger(rsaBlock, true, true);
                    var encryptedBigInt = BigInteger.ModPow(rsaBigInt, exponent.Value, modulus.Value);
                    var encryptedBytes = encryptedBigInt.ToByteArray(true, true);
                    buffer.WriteBytes(encryptedBytes);
                }
                else
                {
                    buffer.WriteBytes(rsaBlock);
                }
            }

            buffer.Flip();
            return buffer;
        }
    }
}
