using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Hagalaz.Cache.Extensions;
using Hagalaz.Security;

namespace Hagalaz.Cache
{
    /// <summary>
    /// A <see cref="ChecksumTable"/> stores checksums and versions of
    /// <see cref="ReferenceTable"/>. When encoded in a <see cref="Container"/> and prepended
    /// with the file type and id it is more commonly known as the client's
    /// "update keys".
    /// </summary>
    public class ChecksumTable : IDisposable
    {
        /// <summary>
        /// The files.
        /// </summary>
        private ChecksumTableEntry[] _entries;

        /// <summary>
        /// Contains count of entries.
        /// </summary>
        /// <value>The entry count.</value>
        public int Count
        {
            get
            {
                if (_entries == null)
                {
                    throw new InvalidOperationException($"{nameof(ChecksumTable)} is not decoded");
                }

                return _entries.Length;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChecksumTable"/> class.
        /// </summary>
        /// <param name="entryCount">The entry count.</param>
        public ChecksumTable(int entryCount) => _entries = new ChecksumTableEntry[entryCount];

        /// <summary>
        /// Decodes the <see cref="ChecksumTable" /> in the specified
        /// <see cref="MemoryStream" />. Whirlpool digests are not read and RSA keys are not used.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns></returns>
        public static ChecksumTable Decode(MemoryStream stream) => Decode(stream, false);

        /// <summary>
        /// Decodes the {@link ChecksumTable} in the specified
        /// <see cref="MemoryStream" />. RSA Keys are not used.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="whirlpool">if set to <c>true</c> [whirlpool].</param>
        /// <returns></returns>
        public static ChecksumTable Decode(MemoryStream stream, bool whirlpool) => Decode(stream, whirlpool, BigInteger.MinusOne, BigInteger.MinusOne);

        /// <summary>
        /// Decodes the <see cref="ChecksumTable" /> in the specified
        /// <see cref="MemoryStream" /> and decrypts the final
        /// whirlpool hash.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="whirlpool">if set to <c>true</c> [whirlpool].</param>
        /// <param name="modulus">The private key.</param>
        /// <param name="publicKey">The public key.</param>
        /// <returns></returns>
        /// <exception cref="System.IO.IOException">
        /// Decrypted data is not 65 bytes long.
        /// or
        /// Whirlpool digest mismatch.
        /// </exception>
        public static ChecksumTable Decode(MemoryStream stream, bool whirlpool, BigInteger modulus, BigInteger publicKey)
        {
            /* find out how many entries there are and allocate a new table */
            var table = new ChecksumTable(whirlpool ? (stream.ReadSignedByte() & 0xFF) : (stream.Capacity / 8));

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
                table._entries[i] = new ChecksumTableEntry(crc, version, digest);
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

        /// <summary>
        /// Sets the file.
        /// </summary>
        /// <param name="entryID">The file identifier.</param>
        /// <param name="file">The file.</param>
        /// <exception cref="System.IndexOutOfRangeException"></exception>
        public void SetEntry(int entryID, ChecksumTableEntry file)
        {
            if (entryID < 0 || entryID >= Count)
                throw new IndexOutOfRangeException();
            _entries[entryID] = file;
        }

        /// <summary>
        /// Encodes this <see cref="ChecksumTable"/>.
        /// Whirlpool digests are not encoded.
        /// </summary>
        /// <returns></returns>
        public MemoryStream Encode() => Encode(false);

        /// <summary>
        /// Encodes this <see cref="ChecksumTable"/>.
        /// </summary>
        /// <param name="whirlpool">if set to <c>true</c> [whirlpool].</param>
        /// <returns></returns>
        public MemoryStream Encode(bool whirlpool) => Encode(whirlpool, BigInteger.MinusOne, BigInteger.MinusOne);

        /// <summary>
        /// Encodes this <see cref="ChecksumTable"/> and encrypts the final whirlpool hash.
        /// </summary>
        /// <param name="whirlpool">if set to <c>true</c> [whirlpool].</param>
        /// <param name="modulus">The modulus.</param>
        /// <param name="privateKey">The private key.</param>
        /// <returns></returns>
        public MemoryStream Encode(bool whirlpool, BigInteger modulus, BigInteger privateKey)
        {
            var buffer = new MemoryStream();

            /* as the new whirlpool format is more complicated we must write the number of entries */
            if (whirlpool)
                buffer.WriteByte(Count);

            /* encode the individual entries */
            foreach (var entry in _entries)
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

        /// <summary>
        /// Attempts to dispose the table.
        /// </summary>
        public void Dispose() => Dispose(true);

        /// <summary>
        /// Disposes the object.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed code.</param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _entries = null!;
            }
        }
    }
}