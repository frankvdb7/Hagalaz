using System;
using System.IO;
using Hagalaz.Cache.Utilities;
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
        private FileStore? _fileStore;

        /// <summary>
        /// The reference tables
        /// </summary>
        private ReferenceTable[]? _referenceTables;
        private readonly IOptions<CacheOptions> _options;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// The lower level API file store.
        /// </summary>
        public FileStore Store
        {
            get
            {
                if (_fileStore == null)
                {
                    _fileStore = FileStore.Open(_options.Value.Path, _loggerFactory.CreateLogger<FileStore>());
                    _referenceTables = new ReferenceTable[Store.IndexFileCount];
                }
                return _fileStore;
            }
        }

        /// <summary>
        /// Constructs the cache.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="loggerFactory"></param>
        public CacheApi(IOptions<CacheOptions> options, ILoggerFactory loggerFactory)
        {
            _options = options;
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Get's file id from given name.
        /// </summary>
        /// <param name="indexId">Store(Cache) id from where to search.</param>
        /// <param name="fileName">Name of the file to search for.</param>
        /// <returns>Return's file ID or -1 if no files found with given name.</returns>
        public int GetFileId(int indexId, string fileName) => ReadReferenceTable(indexId).GetFileId(fileName);

        /// <summary>
        /// Get's file count in cache.
        /// </summary>
        /// <param name="indexId">Cache for which to look for.</param>
        /// <returns></returns>
        public int GetFileCount(int indexId) => Store.GetFileCount(indexId);

        /// <summary>
        /// Gets the sub file count.
        /// </summary>
        /// <param name="indexId">The cache identifier.</param>
        /// <param name="fileId">The sub file identifier.</param>
        /// <returns></returns>
        public int GetFileCount(int indexId, int fileId)
        {
            var table = ReadReferenceTable(indexId);
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
        public ReferenceTable ReadReferenceTable(int indexId)
        {
            if (indexId >= Store.IndexFileCount)
                throw new FileNotFoundException();
            if (indexId == 255)
                throw new IOException("Checksum tables can not be read by this method!");
            
            if (_referenceTables == null)
            {
                var referenceTable = DecodeReferenceTable(indexId);
                _referenceTables![indexId] = referenceTable;
                return referenceTable;
            }

            return _referenceTables[indexId] ?? (_referenceTables[indexId] = DecodeReferenceTable(indexId));
        }

        /// <summary>
        /// Decodes the reference table.
        /// </summary>
        /// <param name="indexId">The index identifier.</param>
        /// <returns></returns>
        private ReferenceTable DecodeReferenceTable(int indexId)
        {
            using (var fileBuffer = Read(255, indexId))
            {
                using (var dataBuffer = Container.Decode(fileBuffer).Data)
                {
                    return ReferenceTable.Decode(dataBuffer, true);
                }
            }
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
        public Container ReadContainer(int indexId, int fileId) => Container.Decode(Read(indexId, fileId));

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
            if (indexId != 255 && indexId >= Store.IndexFileCount)
                throw new FileNotFoundException();
            /* we don't want people reading/manipulating these manually */
            if (indexId == 255 && fileId == 255)
                throw new IOException("Checksum tables can only be read with the low level FileStore API!");

            /* delegate the call to the file store */
            return Store.Read(indexId, fileId);
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
            if (indexId >= Store.IndexFileCount)
                throw new FileNotFoundException();
            var table = ReadReferenceTable(indexId);
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
            if (indexId >= Store.IndexFileCount)
                throw new FileNotFoundException();
            var table = ReadReferenceTable(indexId);
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
        public Container ReadContainer(int indexId, int fileId, int[] xteaKeys)
        {
            /* we don't want people reading/manipulating these manually */
            if (indexId == 255 && fileId == 255)
                throw new IOException("Checksum tables can only be read with the low level FileStore API!");

            /* read the full compressed file */
            using (var compressedFile = Store.Read(indexId, fileId))
            {
                /* if the xtea keys are not defined, return decoded container data as is */
                if (xteaKeys[0] == 0 && xteaKeys[1] == 0 && xteaKeys[2] == 0 && xteaKeys[3] == 0)
                    return Container.Decode(compressedFile);

                /* decrypt the compressed stream, starting at offset 5 to skip the header */
                var buffer = compressedFile.ToArray();
                var decrypted = XteaDecryptor.DecryptXtea(xteaKeys, buffer, 5, buffer.Length);

                /* decode and return the decrypted data */
                using (var stream = new MemoryStream(decrypted))
                {
                    /* decode and return the decrypted data */
                    return Container.Decode(stream);
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
        public void Write(int indexId, int fileId, Container container)
        {
            /* we don't want people reading/manipulating these manually */
            if (indexId == 255 && fileId == 255)
                throw new IOException("Checksum tables can only be read with the low level FileStore API!");

            /* increment the container's version */
            container.Version++;

            /* decode the reference table for this index */
            var tableContainer = Container.Decode(Store.Read(255, indexId));
            var table = ReferenceTable.Decode(tableContainer.Data, true);

            /* update the version and checksum for this file */
            var entry = table.GetEntry(fileId);
            if (entry == null)
            {
                /* create a new entry for the file */
                entry = new ReferenceTableEntry(fileId);
                table.AddEntry(fileId, entry);
            }

            entry.Version = container.Version;

            /* grab the bytes we need for the checksum */
            var bytes = container.Encode(false);

            /* calculate the new CRC checksum */
            entry.Crc32 = BufferUtilities.GetCrcChecksum(bytes, 0, bytes.Length);

            /* calculate and update the whirlpool digest if we need to */
            var whirlpool = new byte[64];
            if (table.Flags.HasFlag(ReferenceTableFlags.Digests))
                whirlpool = Whirlpool.GenerateDigest(bytes, 0, bytes.Length);
            entry.WhirlpoolDigest = whirlpool;

            /* update the reference table version */
            table.Version++;

            /* save the reference table */
            using (var referenceTableContainer = new Container(tableContainer.CompressionType, table.Encode()))
            {
                using (MemoryStream refStream = new MemoryStream(referenceTableContainer.Encode()))
                    Store.Write(255, indexId, refStream);
            }

            /* save the file itself */
            using (MemoryStream fileStream = new MemoryStream(bytes))
                Store.Write(indexId, fileId, fileStream);
        }

        /// <summary>
        /// Computes the <see cref="ChecksumTable"/> for this cache. The checksum table
        /// forms part of the so-called "update keys".
        /// </summary>
        /// <returns></returns>
        public ChecksumTable CreateChecksumTable()
        {
            /* create the checksum table */
            int size = Store.IndexFileCount;
            var table = new ChecksumTable(size);

            /* loop through all the reference tables and get their CRC and versions */
            for (var cacheId = 0; cacheId < size; cacheId++)
            {
                int crc32 = 0;
                int version = 0;
                var whirlpool = new byte[64];

                using (var buffer = Store.Read(255, cacheId))
                {
                    // if there is actually a reference table, calculate the CRC,
                    // version and whirlpool hash
                    if (buffer.Length > 0)
                    {
                        using (var dataStream = Container.Decode(buffer).Data)
                        {
                            var referenceTable = ReferenceTable.Decode(dataStream, false);

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
        public void Dispose() => Dispose(true);

        /// <summary>
        /// Disposes the object.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed code.</param>
        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _fileStore?.Dispose();
            _fileStore = null;
            _referenceTables = null;
        }
    }
}