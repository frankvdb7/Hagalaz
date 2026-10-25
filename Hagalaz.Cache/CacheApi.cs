using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Metrics;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Utilities;
using Hagalaz.Security;
using Microsoft.Extensions.Options;

namespace Hagalaz.Cache
{
    /// <summary>
    /// The <see cref="CacheApi" /> class provides a unified, high-level API for reading from and writing to
    /// the cache of a RSPS game, abstracting the underlying file store and data structures.
    /// </summary>
    public class CacheApi : IDisposable, ICacheAPI
    {
        private readonly IFileStore _store;
        private readonly ICacheWriter _cacheWriter;
        private readonly IContainerDecoder _containerFactory;
        private readonly IArchiveDecoder _archiveDecoder;
        private readonly ArchiveEncoder _archiveEncoder = new();
        private readonly IReferenceTableCodec _referenceTableFactory;
        private readonly ReaderWriterLockSlim _snapshotReadWriteLock = new(LockRecursionPolicy.NoRecursion);
        private readonly ArchiveSnapshotLoader _archiveSnapshotLoader;
        private readonly ArchiveSnapshotCache _archiveSnapshotCache;
        private readonly ReferenceTableCache _referenceTableCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheApi"/> class.
        /// </summary>
        public CacheApi(
            IFileStore store, IReferenceTableProvider referenceTableProvider, ICacheWriter cacheWriter, IContainerDecoder containerFactory,
            IReferenceTableCodec referenceTableFactory, IArchiveDecoder archiveDecoder, IOptions<CacheOptions> options)
        {
            _store = store;
            _cacheWriter = cacheWriter;
            _containerFactory = containerFactory;
            _referenceTableFactory = referenceTableFactory;
            _archiveDecoder = archiveDecoder;
            _archiveSnapshotLoader = new ArchiveSnapshotLoader(store, containerFactory, archiveDecoder);
            _archiveSnapshotCache = new ArchiveSnapshotCache(
                options.Value.ArchiveSnapshotCacheSizeBytes,
                _archiveSnapshotLoader);
            _referenceTableCache = new ReferenceTableCache(referenceTableProvider, store.IndexFileCount);
        }

        /// <summary>
        /// Gets the identifier of a file within a cache index by its name hash.
        /// </summary>
        /// <param name="indexId">The identifier of the index (cache) to search within.</param>
        /// <param name="fileName">The name of the file to find.</param>
        /// <returns>The file's identifier, or -1 if no file with the given name is found.</returns>
        public int GetFileId(int indexId, string fileName) => ReadReferenceTable(indexId).GetFileId(fileName);

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
            var table = ReadReferenceTable(indexId);
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
            _snapshotReadWriteLock.EnterReadLock();
            try
            {
                return _referenceTableCache.Get(indexId);
            }
            finally
            {
                _snapshotReadWriteLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Reads a file from the cache and decodes it into a container, which includes metadata and the file's data.
        /// </summary>
        /// <param name="indexId">The identifier of the index (cache) from which to read.</param>
        /// <param name="fileId">The identifier of the file to read.</param>
        /// <returns>A decoded <see cref="IContainer"/> containing the file's data.</returns>
        public IContainer ReadContainer(int indexId, int fileId)
        {
            return _archiveSnapshotLoader.ReadContainer(indexId, fileId);
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
            if (indexId != 255 && indexId >= _store.IndexFileCount) throw new FileNotFoundException();
            /* we don't want people reading/manipulating these manually */
            if (indexId == 255 && fileId == 255) throw new IOException("Checksum tables can only be read with the low level FileStore API!");

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
        public IArchive ReadArchive(int indexId, int fileId)
        {
            /* check if the index is valid */
            if (indexId >= _store.IndexFileCount) throw new FileNotFoundException();
            var table = ReadReferenceTable(indexId);
            if (fileId < 0 || fileId >= table.Capacity) throw new FileNotFoundException();
            var entry = table.GetEntry(fileId);
            if (entry == null) throw new FileNotFoundException();

            var loadStart = Stopwatch.GetTimestamp();
            var loadSucceeded = false;
            try
            {
                IArchive archive;
                using (var container = ReadContainer(indexId, fileId))
                {
                    /* decode the archive from the container */
                    archive = _archiveDecoder.Decode(container, entry.Capacity);
                }

                loadSucceeded = true;
                return archive;
            }
            finally
            {
                CacheMetrics.RecordArchiveLoad(loadSucceeded, Stopwatch.GetElapsedTime(loadStart).TotalSeconds);
            }
        }

        /// <summary>
        /// Replaces one member of an archive and persists the rebuilt archive under the cache write lock.
        /// </summary>
        /// <param name="indexId">The index containing the archive.</param>
        /// <param name="fileId">The archive identifier.</param>
        /// <param name="archiveEntryId">The member identifier from the reference table.</param>
        /// <param name="replacement">The replacement member bytes.</param>
        public void ReplaceArchiveMember(int indexId, int fileId, int archiveEntryId, ReadOnlyMemory<byte> replacement)
        {
            _snapshotReadWriteLock.EnterWriteLock();
            try
            {
                var tableEntry = _referenceTableCache.Get(indexId).GetEntry(fileId);
                var childEntry = tableEntry?.GetEntry(archiveEntryId);
                if (tableEntry is null || childEntry is null)
                {
                    throw new FileNotFoundException($"Unable to locate archive entry {archiveEntryId} in archive {fileId}.");
                }

                var (container, archive) = ReadArchiveData(indexId, fileId, tableEntry);
                using (container)
                using (archive)
                {
                    var entries = archive.Entries ?? throw new InvalidDataException("Archive entries are not available.");
                    entries[childEntry.Index].Dispose();
                    entries[childEntry.Index] = new MemoryStream(replacement.ToArray());

                    using var archiveData = _archiveEncoder.Encode(archive);
                    using var updatedContainer = new Container(container.CompressionType, archiveData, container.Version);
                    WriteAndInvalidateUnderLock(indexId, fileId, updatedContainer);
                }
            }
            finally
            {
                _snapshotReadWriteLock.ExitWriteLock();
            }
        }

        private (IContainer Container, IArchive Archive) ReadArchiveData(int indexId, int fileId, IReferenceTableEntry tableEntry)
        {
            var loadStart = Stopwatch.GetTimestamp();
            var loadSucceeded = false;
            IContainer? container = null;
            try
            {
                container = ReadContainer(indexId, fileId);
                var archive = _archiveDecoder.Decode(container, tableEntry.Capacity);
                loadSucceeded = true;
                return (container, archive);
            }
            catch
            {
                container?.Dispose();
                throw;
            }
            finally
            {
                CacheMetrics.RecordArchiveLoad(loadSucceeded, Stopwatch.GetElapsedTime(loadStart).TotalSeconds);
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
            _snapshotReadWriteLock.EnterReadLock();
            try
            {
                /* check if the file/subfile are valid */
                if (indexId >= _store.IndexFileCount) throw new FileNotFoundException();
                var table = _referenceTableCache.Get(indexId);
                if (fileId < 0 || fileId >= table.Capacity) throw new FileNotFoundException();
                var entry = table.GetEntry(fileId);
                if (entry == null || subFileId < 0 || subFileId >= entry.Capacity) throw new FileNotFoundException();

                return _archiveSnapshotCache.ReadMember(
                    new ArchiveKey(indexId, fileId),
                    subFileId,
                    entry.Capacity);
            }
            finally
            {
                _snapshotReadWriteLock.ExitReadLock();
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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="xteaKeys"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="xteaKeys"/> does not contain exactly four keys.</exception>
        public IContainer ReadContainer(int indexId, int fileId, int[] xteaKeys)
        {
            return _archiveSnapshotLoader.ReadContainer(indexId, fileId, xteaKeys);
        }

        /// <summary>
        /// Writes a data container to a specific file in the cache.
        /// </summary>
        /// <param name="indexId">The identifier of the index (cache) to write to.</param>
        /// <param name="fileId">The identifier of the file to write.</param>
        /// <param name="container">The container holding the data to be written.</param>
        public void Write(int indexId, int fileId, IContainer container)
        {
            _snapshotReadWriteLock.EnterWriteLock();
            try
            {
                WriteAndInvalidateUnderLock(indexId, fileId, container);
            }
            finally
            {
                _snapshotReadWriteLock.ExitWriteLock();
            }
        }

        private void WriteAndInvalidateUnderLock(int indexId, int fileId, IContainer container)
        {
            try
            {
                _cacheWriter.Write(indexId, fileId, container);
            }
            finally
            {
                _referenceTableCache.Clear(indexId);
                _archiveSnapshotCache.Invalidate(new ArchiveKey(indexId, fileId));
            }
        }

        /// <summary>
        /// Writes raw file bytes without changing container versions or reference-table metadata.
        /// </summary>
        public void WriteRawFile(int indexId, int fileId, MemoryStream data)
        {
            _snapshotReadWriteLock.EnterWriteLock();
            try
            {
                try
                {
                    _store.Write(indexId, fileId, data);
                }
                finally
                {
                    InvalidateRawMutation(indexId, fileId);
                }
            }
            finally
            {
                _snapshotReadWriteLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Copies raw file bytes without changing container versions or reference-table metadata.
        /// </summary>
        public void CopyRawFile(int indexId, int fileId, int sourceIndexId, int sourceFileId)
        {
            _snapshotReadWriteLock.EnterWriteLock();
            try
            {
                try
                {
                    using var source = _store.Read(sourceIndexId, sourceFileId);
                    _store.Write(indexId, fileId, source);
                }
                finally
                {
                    InvalidateRawMutation(indexId, fileId);
                }
            }
            finally
            {
                _snapshotReadWriteLock.ExitWriteLock();
            }
        }

        private void InvalidateRawMutation(int indexId, int fileId)
        {
            if (indexId == 255)
            {
                if (fileId >= 0 && fileId < _store.IndexFileCount && fileId != 255)
                {
                    _referenceTableCache.Clear(fileId);
                    _archiveSnapshotCache.InvalidateIndex(fileId);
                }

                return;
            }

            _archiveSnapshotCache.Invalidate(new ArchiveKey(indexId, fileId));
        }

        /// <summary>
        /// Computes the <see cref="ChecksumTable"/> for the entire cache. The checksum table
        /// forms part of the "update keys" used by the game client to verify cache integrity.
        /// </summary>
        /// <returns>The generated <see cref="ChecksumTable"/>.</returns>
        public IChecksumTable CreateChecksumTable()
        {
            _snapshotReadWriteLock.EnterReadLock();
            try
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
            finally
            {
                _snapshotReadWriteLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Disposes resources owned by this cache API instance.
        /// </summary>
        /// <remarks>
        /// The injected file store is externally owned and must be disposed by its composition root.
        /// Retained snapshot bytes are reconciled with the process-wide meter before this instance is disposed.
        /// </remarks>
        public void Dispose()
        {
            _snapshotReadWriteLock.EnterWriteLock();
            try
            {
                _archiveSnapshotCache.Clear();
            }
            finally
            {
                _snapshotReadWriteLock.ExitWriteLock();
                _snapshotReadWriteLock.Dispose();
            }
        }
    }
}
