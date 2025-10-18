using System;

namespace Hagalaz.Cache
{
    /// <summary>
    /// Represents a single entry in a <see cref="ChecksumTable"/>, containing the CRC32 checksum, version,
    /// and optionally a whirlpool digest for a specific cache index's reference table.
    /// </summary>
    public class ChecksumTableEntry
    {
        /// <summary>
        /// Gets the CRC32 checksum of the reference table.
        /// </summary>
        public int Crc32 { get; }

        /// <summary>
        /// Gets the version of the reference table.
        /// </summary>
        public int Version { get; }

        /// <summary>
        /// Gets the 64-byte whirlpool digest of the reference table.
        /// </summary>
        public byte[] Digest { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChecksumTableEntry" /> class.
        /// </summary>
        /// <param name="crc32">The CRC32 checksum of the reference table.</param>
        /// <param name="version">The version of the reference table.</param>
        /// <param name="digest">The 64-byte whirlpool digest of the reference table.</param>
        /// <exception cref="ArgumentException">Thrown if the provided digest is not 64 bytes long.</exception>
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
