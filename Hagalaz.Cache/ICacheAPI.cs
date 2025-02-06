using System.IO;

namespace Hagalaz.Cache
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICacheAPI
    {
        /// <summary>
        /// Get's file id from given name.
        /// </summary>
        /// <param name="indexId">Store(Cache) id from where to search.</param>
        /// <param name="fileName">Name of the file to search for.</param>
        /// <returns>Return's file ID or -1 if no files found with given name.</returns>
        int GetFileId(int indexId, string fileName);
        /// <summary>
        /// Get's file count in cache.
        /// </summary>
        /// <param name="indexId">Cache for which to look for.</param>
        /// <returns></returns>
        int GetFileCount(int indexId);
        /// <summary>
        /// Gets the sub file count.
        /// </summary>
        /// <param name="indexId">The cache identifier.</param>
        /// <param name="fileId">The sub file identifier.</param>
        /// <returns></returns>
        int GetFileCount(int indexId, int fileId);
        /// <summary>
        /// Gets the reference table.
        /// </summary>
        /// <param name="indexId">The index identifier.</param>
        /// <returns></returns>
        ReferenceTable ReadReferenceTable(int indexId);
        /// <summary>
        /// Reads a container from the cache.
        /// </summary>
        /// <param name="indexId">The cache identifier.</param>
        /// <param name="fileId">The file identifier.</param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="IOException">Checksum tables can only be read with the low level FileStore API!</exception>
        /// <exception cref="System.IO.IOException">Checksum tables can only be read with the low level FileStore API!</exception>
        Container ReadContainer(int indexId, int fileId);
        /// <summary>
        /// Reads the archive.
        /// </summary>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="fileId">The file identifier.</param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException">
        /// </exception>
        Archive ReadArchive(int indexId, int fileId);
        /// <summary>
        /// Reads a file contained in an archive in the cache.
        /// </summary>
        /// <param name="indexId">The cache identifier.</param>
        /// <param name="fileId">The file identifier.</param>
        /// <param name="subFileId">The sub file identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.IO.FileNotFoundException"></exception>
        MemoryStream Read(int indexId, int fileId, int subFileId);
        /// <summary>
        /// Reads a file from the cache with the specified xtea keys.
        /// </summary>
        /// <param name="indexId">The cache identifier.</param>
        /// <param name="fileId">The file identifier.</param>
        /// <param name="xteaKeys">The xtea keys.</param>
        /// <returns></returns>
        /// <exception cref="System.IO.IOException">Checksum tables can only be read with the low level FileStore API!</exception>
        Container ReadContainer(int indexId, int fileId, int[] xteaKeys);
        /// <summary>
        /// Reads a raw container stream (not decoded) from the cache.
        /// </summary>
        /// <param name="indexId">The cache identifier.</param>
        /// <param name="fileId">The file identifier.</param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="IOException">Checksum tables can only be read with the low level FileStore API!</exception>
        /// <exception cref="System.IO.IOException">Checksum tables can only be read with the low level FileStore API!</exception>
        MemoryStream Read(int indexId, int fileId);
        /// <summary>
        /// Writes the specified index identifier.
        /// </summary>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="fileId">The file identifier.</param>
        /// <param name="container">The container.</param>
        /// <exception cref="System.IO.IOException">Checksum tables can only be read with the low level FileStore API!</exception>
        void Write(int indexId, int fileId, Container container);
        /// <summary>
        /// Computes the <see cref="ChecksumTable"/> for this cache. The checksum table
        /// forms part of the so-called "update keys".
        /// </summary>
        /// <returns></returns>
        ChecksumTable CreateChecksumTable();
    }
}
