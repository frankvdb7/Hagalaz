namespace Hagalaz.Cache.Abstractions.Model
{
    /// <summary>
    /// Defines the structure of a sector, which is a fundamental unit of data storage in the cache file system.
    /// Each sector acts as a header for a block of data, containing metadata to locate and link file chunks.
    /// </summary>
    public interface ISector
    {
        /// <summary>
        /// Gets the ID of the cache index (also known as an archive or store) to which this sector belongs.
        /// </summary>
        int IndexID { get; }

        /// <summary>
        /// Gets the ID of the file to which this sector's data chunk belongs.
        /// </summary>
        int FileID { get; }

        /// <summary>
        /// Gets the sequential ID of the data chunk within the file that this sector represents.
        /// A file can be split across multiple chunks and, therefore, multiple sectors.
        /// </summary>
        int ChunkID { get; }

        /// <summary>
        /// Gets the ID of the next sector in the chain for the current file.
        /// If this is the last chunk of the file, this value points to a terminator or zero.
        /// </summary>
        int NextSectorID { get; }
    }
}