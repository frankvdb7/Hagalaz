using System.IO;

namespace Hagalaz.Cache
{
    /// <summary>
    /// Defines the contract for a high-level API to interact with the game cache,
    /// providing methods for reading, writing, and managing cache files.
    /// </summary>
    public interface ICacheAPI
    {
        /// <summary>
        /// Gets the identifier of a file within a cache index by its name hash.
        /// </summary>
        /// <param name="indexId">The identifier of the index (cache) to search within.</param>
        /// <param name="fileName">The name of the file to find.</param>
        /// <returns>The file's identifier, or -1 if no file with the given name is found.</returns>
        int GetFileId(int indexId, string fileName);

        /// <summary>
        /// Gets the total number of files in a specified cache index.
        /// </summary>
        /// <param name="indexId">The identifier of the index (cache) to count files in.</param>
        /// <returns>The total number of files in the index.</returns>
        int GetFileCount(int indexId);

        /// <summary>
        /// Gets the number of member files (sub-files) within an archive.
        /// </summary>
        /// <param name="indexId">The identifier of the index (cache) containing the archive.</param>
        /// <param name="fileId">The identifier of the archive file.</param>
        /// <returns>The number of member files in the specified archive.</returns>
        int GetFileCount(int indexId, int fileId);

        /// <summary>
        /// Reads the reference table for a specific cache index.
        /// </summary>
        /// <param name="indexId">The identifier of the index whose reference table is to be read.</param>
        /// <returns>The decoded <see cref="IReferenceTable"/> for the specified index.</returns>
        IReferenceTable ReadReferenceTable(int indexId);

        /// <summary>
        /// Reads a file from the cache and decodes it into a container.
        /// </summary>
        /// <param name="indexId">The identifier of the index from which to read.</param>
        /// <param name="fileId">The identifier of the file to read.</param>
        /// <returns>A decoded <see cref="IContainer"/> containing the file's data.</returns>
        IContainer ReadContainer(int indexId, int fileId);

        /// <summary>
        /// Reads an archive file from the cache and decodes it.
        /// </summary>
        /// <param name="indexId">The identifier of the index containing the archive.</param>
        /// <param name="fileId">The identifier of the archive file to read.</param>
        /// <returns>A decoded <see cref="Archive"/> with its member file entries populated.</returns>
        Archive ReadArchive(int indexId, int fileId);

        /// <summary>
        /// Reads a single member file from within an archive in the cache.
        /// </summary>
        /// <param name="indexId">The identifier of the index containing the archive.</param>
        /// <param name="fileId">The identifier of the archive file.</param>
        /// <param name="subFileId">The identifier of the member file to read from the archive.</param>
        /// <returns>A <see cref="MemoryStream"/> containing the data of the requested member file.</returns>
        MemoryStream Read(int indexId, int fileId, int subFileId);

        /// <summary>
        /// Reads a file from the cache, decrypting it using the provided XTEA keys.
        /// </summary>
        /// <param name="indexId">The identifier of the index from which to read.</param>
        /// <param name="fileId">The identifier of the file to read.</param>
        /// <param name="xteaKeys">The set of four integer keys used for XTEA decryption.</param>
        /// <returns>A decoded <see cref="IContainer"/> containing the decrypted file data.</returns>
        IContainer ReadContainer(int indexId, int fileId, int[] xteaKeys);

        /// <summary>
        /// Reads the raw, compressed data of a file from the cache into a memory stream.
        /// </summary>
        /// <param name="indexId">The identifier of the index from which to read.</param>
        /// <param name="fileId">The identifier of the file to read.</param>
        /// <returns>A <see cref="MemoryStream"/> containing the raw file data.</returns>
        MemoryStream Read(int indexId, int fileId);

        /// <summary>
        /// Writes a data container to a specific file in the cache.
        /// </summary>
        /// <param name="indexId">The identifier of the index to write to.</param>
        /// <param name="fileId">The identifier of the file to write.</param>
        /// <param name="container">The container holding the data to be written.</param>
        void Write(int indexId, int fileId, IContainer container);

        /// <summary>
        /// Computes the <see cref="ChecksumTable"/> for the entire cache.
        /// </summary>
        /// <returns>The generated <see cref="ChecksumTable"/>.</returns>
        ChecksumTable CreateChecksumTable();
    }
}
