using System;
using System.IO;
using Hagalaz.Cache.Utilities;
using Hagalaz.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hagalaz.Cache
{
    /// <summary>
    /// The <see cref="CacheApi" /> class provides a unified, high-level API for reading from and writing to
    /// the cache of a Jagex game, abstracting the underlying file store and data structures.
    /// </summary>
    public class CacheApi : IDisposable, ICacheAPI
    {
        private readonly IFileStore _store;
        private readonly IReferenceTableProvider _referenceTableProvider;
        private readonly ICacheWriter _cacheWriter;
        private readonly IContainerDecoder _containerFactory;
        private readonly IArchiveDecoder _archiveDecoder;
        private readonly IReferenceTableCodec _referenceTableFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheApi"/> class.
        /// </summary>
        public CacheApi(IFileStore store, IReferenceTableProvider referenceTableProvider, ICacheWriter cacheWriter, IContainerDecoder containerFactory, IReferenceTableCodec referenceTableFactory, IArchiveDecoder archiveDecoder)
        {
            _store = store;
            _referenceTableProvider = referenceTableProvider;
            _cacheWriter = cacheWriter;
            _containerFactory = containerFactory;
            _referenceTableFactory = referenceTableFactory;
            _archiveDecoder = archiveDecoder;
        }

        /// <summary>
        /// Gets the identifier of a file within a cache index by its name hash.
        /// </summary>
        /// <param name="indexId">The identifier of the index (cache) to search within.</param>
        /// <param name="fileName">The name of the file to find.</param>
        /// <returns>The file's identifier, or -1 if no file with the given name is found.</returns>
        public int GetFileId(int indexId, string fileName) => _referenceTableProvider.ReadReferenceTable(indexId).GetFileId(fileName);

        /// <summary>
        /// Gets the total number of files in a specified cache index.
        /// </summary>
        /// <param name="indexId">The identifier of the index (cache) to count files in.</param>
        /// <returns>The total number of files in the index.</returns>
        public int GetFileCount(int indexId) => _store.GetFileCount(indexId);

        /// <summary>
        /// Gets the number of member files (sub-files) within an archive.
        /// </summary>
        /// <param name="indexId">The identifier of the index (cache) containing the archive.</param>
        /// <param name="fileId">The identifier of the archive file.</param>
        /// <returns>The number of member files in the specified archive.</returns>
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
        /// Reads the reference table for a specific cache index. The reference table contains metadata about all files in that index.
        /// </summary>
        /// <param name="indexId">The identifier of the index whose reference table is to be read.</param>
        /// <returns>The decoded <see cref="IReferenceTable"/> for the specified index.</returns>
        public IReferenceTable ReadReferenceTable(int indexId)
        {
            return _referenceTableProvider.ReadReferenceTable(indexId);
        }

        /// <summary>
        /// Reads a file from the cache and decodes it into a container, which includes metadata and the file's data.
        /// </summary>
        /// <param name="indexId">The identifier of the index (cache) from which to read.</param>
        /// <param name="fileId">The identifier of the file to read.</param>
        /// <returns>A decoded <see cref="IContainer"/> containing the file's data.</returns>
        public IContainer ReadContainer(int indexId, int fileId)
        {
            using var stream = Read(indexId, fileId);
            return _containerFactory.Decode(stream);
        }

        /// <summary>
        /// Reads the raw, compressed data of a file from the cache into a memory stream.
        /// </summary>
        /// <param name="indexId">The identifier of the index (cache) from which to read.</param>
        /// <param name="fileId">The identifier of the file to read.</param>
        /// <returns>A <see cref="MemoryStream"/> containing the raw file data.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the specified index is invalid.</exception>
        /// <exception cref="IOException">Thrown if attempting to read the master checksum table, which should be accessed via other methods.</exception>
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
        /// Reads an archive file from the cache and decodes it, providing access to its member files.
        /// </summary>
        /// <param name="indexId">The identifier of the index (cache) containing the archive.</param>
        /// <param name="fileId">The identifier of the archive file to read.</param>
        /// <returns>A decoded <see cref="Archive"/> with its member file entries populated.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the index, file, or archive entry does not exist.</exception>
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
                return _archiveDecoder.Decode(container, entry.Capacity);
            }
        }

        /// <summary>
        /// Reads a single member file from within an archive in the cache.
        /// </summary>
        /// <param name="indexId">The identifier of the index (cache) containing the archive.</param>
        /// <param name="fileId">The identifier of the archive file.</param>
        /// <param name="subFileId">The identifier of the member file to read from the archive.</param>
        /// <returns>A <see cref="MemoryStream"/> containing the data of the requested member file.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the index, file, archive entry, or sub-file does not exist.</exception>
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
                var archive = _archiveDecoder.Decode(container, entry.Capacity);
                return archive.GetEntry(subFileId);
            }
        }

        /// <summary>
        /// Reads a file from the cache, decrypting it using the provided XTEA keys.
        /// </summary>
        /// <param name="indexId">The identifier of the index (cache) from which to read.</param>
        /// <param name="fileId">The identifier of the file to read.</param>
        /// <param name="xteaKeys">The set of four integer keys used for XTEA decryption.</param>
        /// <returns>A decoded <see cref="IContainer"/> containing the decrypted file data.</returns>
        /// <exception cref="IOException">Thrown if attempting to read the master checksum table.</exception>
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
        /// Writes a data container to a specific file in the cache.
        /// </summary>
        /// <param name="indexId">The identifier of the index (cache) to write to.</param>
        /// <param name="fileId">The identifier of the file to write.</param>
        /// <param name="container">The container holding the data to be written.</param>
        public void Write(int indexId, int fileId, IContainer container)
        {
            _cacheWriter.Write(indexId, fileId, container);
        }

        /// <summary>
        /// Computes the <see cref="ChecksumTable"/> for the entire cache. The checksum table
        /// forms part of the "update keys" used by the game client to verify cache integrity.
        /// </summary>
        /// <returns>The generated <see cref="ChecksumTable"/>.</returns>
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
        /// Disposes of the underlying file store, releasing any file handles.
        /// </summary>
        public void Dispose()
        {
            _store.Dispose();
        }
    }
}