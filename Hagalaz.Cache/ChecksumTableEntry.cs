using System;

namespace Hagalaz.Cache
{
    /// <summary>
    /// 
    /// </summary>
    public class ChecksumTableEntry
    {
        /// <summary>
        /// Contains file crc32.
        /// </summary>
        public int Crc32 { get; }
        /// <summary>
        /// Contains the version.
        /// </summary>
        public int Version { get; }
        /// <summary>
        /// Contains whirlpool digest of this file.
        /// </summary>
        public byte[] Digest { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChecksumTableEntry" /> class.
        /// </summary>
        /// <param name="crc32">The CRC32.</param>
        /// <param name="version">The version.</param>
        /// <param name="digest">The digest.</param>
        /// <exception cref="System.ArgumentException">Whirlpool digest should be 64 bytes long.</exception>
        public ChecksumTableEntry(int crc32, int version, byte[] digest)
        {
            if (digest.Length != 64)
                throw new ArgumentException("Whirlpool digest should be 64 bytes long.");
            Crc32 = crc32;
            Version = version;
            Digest = digest;
        }
    }
}
