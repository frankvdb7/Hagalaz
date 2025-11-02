namespace Hagalaz.Cache.Abstractions.Model
{
    public interface IChecksumTableEntry
    {
        /// <summary>
        /// Gets the CRC32 checksum of the reference table.
        /// </summary>
        int Crc32 { get; }

        /// <summary>
        /// Gets the version of the reference table.
        /// </summary>
        int Version { get; }

        /// <summary>
        /// Gets the 64-byte whirlpool digest of the reference table.
        /// </summary>
        byte[] Digest { get; }
    }
}