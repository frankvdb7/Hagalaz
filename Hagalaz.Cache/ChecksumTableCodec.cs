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
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;

            if (!whirlpool)
            {
                var table = new ChecksumTable((int)ms.Length / 8);
                for (var i = 0; i < table.Count; i++)
                {
                    int crc = ms.ReadInt();
                    int version = ms.ReadInt();
                    table.SetEntry(i, new ChecksumTableEntry(crc, version, null));
                }
                return table;
            }

            int entryCount = ms.ReadByte();
            var whirlpoolTable = new ChecksumTable(entryCount);

            using var tableDataStream = new MemoryStream();
            tableDataStream.WriteByte((byte)entryCount);

            var tempBuffer = new byte[72]; // 4+4+64
            for (var i = 0; i < entryCount; i++)
            {
                ms.Read(tempBuffer, 0, tempBuffer.Length);
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
                var encryptedBytes = new byte[ms.Length - ms.Position];
                ms.Read(encryptedBytes, 0, encryptedBytes.Length);

                var encryptedBigInt = new BigInteger(encryptedBytes, isUnsigned: false, isBigEndian: true);
                var decryptedBigInt = BigInteger.ModPow(encryptedBigInt, exponent.Value, modulus.Value);
                var decryptedBytes = decryptedBigInt.ToByteArray(isUnsigned: false, isBigEndian: true);

                int paddingIndex = -1;
                for (int i = 0; i < decryptedBytes.Length; i++)
                {
                    if (decryptedBytes[i] == 10)
                    {
                        paddingIndex = i;
                        break;
                    }
                }

                if (paddingIndex == -1)
                {
                    throw new IOException("Invalid RSA padding: magic byte not found.");
                }

                var actualDigest = new byte[64];
                if (decryptedBytes.Length - (paddingIndex + 1) < 64)
                {
                    throw new IOException("Invalid RSA block: not enough data for digest.");
                }
                Array.Copy(decryptedBytes, paddingIndex + 1, actualDigest, 0, 64);

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
                    var rsaBigInt = new BigInteger(rsaBlock, isUnsigned: false, isBigEndian: true);
                    var encryptedBigInt = BigInteger.ModPow(rsaBigInt, exponent.Value, modulus.Value);
                    var finalEncryptedBytes = encryptedBigInt.ToByteArray(isUnsigned: false, isBigEndian: true);

                    buffer.WriteBytes(finalEncryptedBytes);
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
