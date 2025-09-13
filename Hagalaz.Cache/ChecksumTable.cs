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
        internal ChecksumTableEntry[] _entries;

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