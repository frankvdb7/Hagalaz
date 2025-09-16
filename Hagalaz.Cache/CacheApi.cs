using System;
using System.IO;
using Hagalaz.Cache.Utilities;
using Hagalaz.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hagalaz.Cache
{
    /// <summary>
    /// The <see cref="CacheApi" /> class provides a unified, high-level API for modifying
    /// the cache of a Jagex game.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class CacheApi : IDisposable, ICacheAPI
    {
        private readonly IFileStore _store;
        private readonly IReferenceTableProvider _referenceTableProvider;
        private readonly ICacheWriter _cacheWriter;
        private readonly IContainerDecoder _containerFactory;

        private readonly IReferenceTableDecoder _referenceTableFactory;

        public CacheApi(IFileStore store, IReferenceTableProvider referenceTableProvider, ICacheWriter cacheWriter, IContainerDecoder containerFactory, IReferenceTableDecoder referenceTableFactory)
        {
            _store = store;
            _referenceTableProvider = referenceTableProvider;
            _cacheWriter = cacheWriter;
            _containerFactory = containerFactory;
            _referenceTableFactory = referenceTableFactory;
        }

        /// <summary>
        /// Get's file id from given name.
        /// </summary>
        /// <param name="indexId">Store(Cache) id from where to search.</param>
        /// <param name="fileName">Name of the file to search for.</param>
        /// <returns>Return's file ID or -1 if no files found with given name.</returns>
        public int GetFileId(int indexId, string fileName) => _referenceTableProvider.ReadReferenceTable(indexId).GetFileId(fileName);

        /// <summary>
        /// Get's file count in cache.
        /// </summary>
        /// <param name="indexId">Cache for which to look for.</param>
        /// <returns></returns>
        public int GetFileCount(int indexId) => _store.GetFileCount(indexId);

        /// <summary>
        /// Gets the sub file count.
        /// </summary>
        /// <param name="indexId">The cache identifier.</param>
        /// <param name="fileId">The sub file identifier.</param>
        /// <returns></returns>
        public int GetFileCount(int indexId, int fileId)
        {
            var table = _referenceTableProvider.ReadReferenceTable(indexId);
            if (fileId < 0 || fileId >= table.Capacity)
            {
                return 0;
            }

            var entry = table.GetEntry(fileId);
            return entry?.Capacity ?? 0;
        }

        /// <summary>
        /// Gets the reference table.
        /// </summary>
        /// <param name="indexId">The index identifier.</param>
        /// <returns></returns>
        public IReferenceTable ReadReferenceTable(int indexId)
        {
            return _referenceTableProvider.ReadReferenceTable(indexId);
        }

        /// <summary>
        /// Reads a container from the cache.
        /// </summary>
        /// <param name="indexId">The cache identifier.</param>
        /// <param name="fileId">The file identifier.</param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="IOException">Checksum tables can only be read with the low level FileStore API!</exception>
        /// <exception cref="System.IO.IOException">Checksum tables can only be read with the low level FileStore API!</exception>
        public IContainer ReadContainer(int indexId, int fileId)
        {
            using var stream = Read(indexId, fileId);
            return _containerFactory.Decode(stream);
        }

        /// <summary>
        /// Reads a container stream (not decoded) from the cache.
        /// </summary>
        /// <param name="indexId">The cache identifier.</param>
        /// <param name="fileId">The file identifier.</param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="IOException">Checksum tables can only be read with the low level FileStore API!</exception>
        /// <exception cref="System.IO.IOException">Checksum tables can only be read with the low level FileStore API!</exception>
        public MemoryStream Read(int indexId, int fileId)
        {
            /* check if the index is valid */
            if (indexId != 255 && indexId >= _store.IndexFileCount)
                throw new FileNotFoundException();
            /* we don't want people reading/manipulating these manually */
            if (indexId == 255 && fileId == 255)
                throw new IOException("Checksum tables can only be read with the low level FileStore API!");

            /* delegate the call to the file store */
            return _store.Read(indexId, fileId);
        }

        /// <summary>
        /// Reads the archive.
        /// </summary>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="fileId">The file identifier.</param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException">
        /// </exception>
        public Archive ReadArchive(int indexId, int fileId)
        {
            /* check if the index is valid */
            if (indexId >= _store.IndexFileCount)
                throw new FileNotFoundException();
            var table = _referenceTableProvider.ReadReferenceTable(indexId);
            if (fileId < 0 || fileId >= table.Capacity)
                throw new FileNotFoundException();
            var entry = table.GetEntry(fileId);
            if (entry == null)
                throw new FileNotFoundException();

            /* grab the container and the reference table sub file count */
            using (var container = ReadContainer(indexId, fileId))
            {
                /* decode the archive from the container */
                return Archive.Decode(container, entry.Capacity);
            }
        }

        /// <summary>
        /// Reads a file contained in an archive in the cache.
        /// </summary>
        /// <param name="indexId">The cache identifier.</param>
        /// <param name="fileId">The file identifier.</param>
        /// <param name="subFileId">The sub file identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.IO.FileNotFoundException"></exception>
        public MemoryStream Read(int indexId, int fileId, int subFileId)
        {
            /* check if the file/subfile are valid */
            if (indexId >= _store.IndexFileCount)
                throw new FileNotFoundException();
            var table = _referenceTableProvider.ReadReferenceTable(indexId);
            if (fileId < 0 || fileId >= table.Capacity)
                throw new FileNotFoundException();
            var entry = table.GetEntry(fileId);
            if (entry == null || subFileId < 0 || subFileId >= entry.Capacity)
                throw new FileNotFoundException();

            /* grab the container and the reference table sub file count */
            using (var container = ReadContainer(indexId, fileId))
            {
                /* extract the entry from the archive */
                var archive = Archive.Decode(container, entry.Capacity);
                return archive.GetEntry(subFileId);
            }
        }

        /// <summary>
        /// Reads a file from the cache with the specified xtea keys.
        /// </summary>
        /// <param name="indexId">The cache identifier.</param>
        /// <param name="fileId">The file identifier.</param>
        /// <param name="xteaKeys">The xtea keys.</param>
        /// <returns></returns>
        /// <exception cref="System.IO.IOException">Checksum tables can only be read with the low level FileStore API!</exception>
        public IContainer ReadContainer(int indexId, int fileId, int[] xteaKeys)
        {
            /* we don't want people reading/manipulating these manually */
            if (indexId == 255 && fileId == 255)
                throw new IOException("Checksum tables can only be read with the low level FileStore API!");

            /* read the full compressed file */
            using (var compressedFile = _store.Read(indexId, fileId))
            {
                /* if the xtea keys are not defined, return decoded container data as is */
                if (xteaKeys[0] == 0 && xteaKeys[1] == 0 && xteaKeys[2] == 0 && xteaKeys[3] == 0)
                    return _containerFactory.Decode(compressedFile);

                /* decrypt the compressed stream, starting at offset 5 to skip the header */
                var buffer = compressedFile.ToArray();
                var decrypted = XteaDecryptor.DecryptXtea(xteaKeys, buffer, 5, buffer.Length);

                /* decode and return the decrypted data */
                using (var stream = new MemoryStream(decrypted))
                {
                    /* decode and return the decrypted data */
                    return _containerFactory.Decode(stream);
                }
            }
        }

        /// <summary>
        /// Writes the specified index identifier.
        /// </summary>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="fileId">The file identifier.</param>
        /// <param name="container">The container.</param>
        /// <exception cref="System.IO.IOException">Checksum tables can only be read with the low level FileStore API!</exception>
        public void Write(int indexId, int fileId, IContainer container)
        {
            _cacheWriter.Write(indexId, fileId, container);
        }

        /// <summary>
        /// Computes the <see cref="ChecksumTable"/> for this cache. The checksum table
        /// forms part of the so-called "update keys".
        /// </summary>
        /// <returns></returns>
        public ChecksumTable CreateChecksumTable()
        {
            /* create the checksum table */
            int size = _store.IndexFileCount;
            var table = new ChecksumTable(size);

            /* loop through all the reference tables and get their CRC and versions */
            for (var cacheId = 0; cacheId < size; cacheId++)
            {
                int crc32 = 0;
                int version = 0;
                var whirlpool = new byte[64];

                using (var buffer = _store.Read(255, cacheId))
                {
                    // if there is actually a reference table, calculate the CRC,
                    // version and whirlpool hash
                    if (buffer.Length > 0)
                    {
                        using (var dataStream = _containerFactory.Decode(buffer).Data)
                        {
                            var referenceTable = _referenceTableFactory.Decode(dataStream, false);

                            var data = buffer.ToArray();
                            version = referenceTable.Version;
                            crc32 = BufferUtilities.GetCrcChecksum(data, 0, data.Length);
                            whirlpool = Whirlpool.GenerateDigest(data, 0, data.Length);
                        }
                    }
                }

                table.SetEntry(cacheId, new ChecksumTableEntry(crc32, version, whirlpool));
            }

            /* return the table */
            return table;
        }

        /// <summary>
        /// Attempts to dispose the cache.
        /// </summary>
        public void Dispose()
        {
            _store.Dispose();
        }
    }
}