using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Cache
{
    /// <summary>
    /// A file store holds multiple files inside a "virtual" file system made up of
    /// several index files and a single data file.
    /// </summary>
    public class FileStore : IDisposable
    {
        /// <summary>
        /// The lock object
        /// </summary>
        private readonly object _lockObj = new object();

        /// <summary>
        /// The data stream
        /// </summary>
        private FileStream _dataFile;

        /// <summary>
        /// The index streams
        /// </summary>
        private FileStream[] _indexFiles;

        /// <summary>
        /// The meta stream
        /// </summary>
        private FileStream _mainIndexFile;

        /// <summary>
        /// Contains the cache file count.
        /// </summary>
        /// <returns></returns>
        public int IndexFileCount => _indexFiles.Length;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileStore" /> class.
        /// </summary>
        /// <param name="dataFile">The data file.</param>
        /// <param name="indexFiles">The index files.</param>
        /// <param name="mainIndexFile">The 'meta' index file.</param>
        private FileStore(FileStream dataFile, FileStream[] indexFiles, FileStream mainIndexFile)
        {
            _dataFile = dataFile;
            _indexFiles = indexFiles;
            _mainIndexFile = mainIndexFile;
        }

        /// <summary>
        /// Opens the file store stored in the specified directory.
        /// </summary>
        /// <param name="rootPath">The directory containing the index and data files.</param>
        /// <returns></returns>
        public static FileStore Open(string rootPath, ILogger<FileStore> logger)
        {
            logger.LogInformation("Cache file store path: {0}", Path.GetFullPath(rootPath));

            /* open the main data file stream */
            var dataFile = File.Open(rootPath + @"/main_file_cache.dat2", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

            /* open all the index file streams */
            var indexFiles = new List<FileStream>();
            for (int i = 0; ; i++)
            {
                var path = rootPath + @"/main_file_cache.idx" + i;
                if (File.Exists(path))
                    indexFiles.Add(File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite));
                else
                    break;
            }

            if (indexFiles.Count == 0) throw new FileNotFoundException("No cache found in directory: " + rootPath);

            /* open the main index file stream */
            var mainIndexFile = File.Open(rootPath + @"/main_file_cache.idx255", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

            /* initialize the store */
            var store = new FileStore(dataFile, indexFiles.ToArray(), mainIndexFile);

            return store;
        }

        /// <summary>
        /// Gets the file count.
        /// </summary>
        /// <param name="indexId">The index identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.IO.FileNotFoundException"></exception>
        public int GetFileCount(int indexId)
        {
            if (indexId >= IndexFileCount && indexId != 255) throw new FileNotFoundException();
            if (indexId == 255) return (int)(_mainIndexFile.Length / Index.IndexSize);
            return (int)(_indexFiles[indexId].Length / Index.IndexSize);
        }

        /// <summary>
        /// Reads a file.
        /// </summary>
        /// <param name="indexId">The cache identifier.</param>
        /// <param name="fileId">The file identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.IO.FileNotFoundException">
        /// </exception>
        /// <exception cref="InvalidDataException">
        /// Index size is invalid.
        /// or
        /// Index sector id is invalid.
        /// or
        /// Invalid sector id.
        /// or
        /// Invalid file id.
        /// or
        /// Invalid cache id.
        /// or
        /// Invalid chunk id.
        /// or
        /// Invalid next sector id.
        /// </exception>
        public MemoryStream Read(int indexId, int fileId)
        {
            if (indexId >= IndexFileCount && indexId != 255) throw new FileNotFoundException();

            var indexStream = indexId == 255 ? _mainIndexFile : _indexFiles[indexId];

            if (((Index.IndexSize * fileId) + Index.IndexSize) > indexStream.Length) throw new FileNotFoundException();

            lock (_lockObj)
            {
                var indexData = ArrayPool<byte>.Shared.Rent(Index.IndexSize);
                try
                {
                    indexStream.Seek(Index.IndexSize * fileId, SeekOrigin.Begin);
                    indexStream.Read(indexData);

                    var index = Index.Decode(indexData);
                    if (index.Size < 0 || index.Size > Sector.MaxFileSize) throw new InvalidDataException("Index size is invalid.");
                    if (index.SectorID <= 0 || (_dataFile.Length / Sector.DataSize) < index.SectorID)
                        throw new InvalidDataException("Index sector id is invalid.");

                    var extended = fileId > ushort.MaxValue;
                    var dataBuffer = new byte[Sector.DataSize];
                    var data = new byte[index.Size];
                    int readBytesCount = 0, currentSectorId = index.SectorID, currentChunkId = 0;
                    var sectorSize = extended ? Sector.ExtendedDataBlockSize : Sector.DataBlockSize;
                    var headerSectorSize = extended ? Sector.ExtendedDataHeaderSize : Sector.DataHeaderSize;
                    while (index.Size > readBytesCount)
                    {
                        if (currentSectorId == 0) throw new InvalidDataException("Invalid sector id.");
                        _dataFile.Seek(Sector.DataSize * currentSectorId, SeekOrigin.Begin);
                        var dataSectorSize = index.Size - readBytesCount;
                        if (dataSectorSize > sectorSize) dataSectorSize = sectorSize;
                        _dataFile.Read(dataBuffer, 0, headerSectorSize + dataSectorSize);

                        var sector = Sector.Decode(dataBuffer, extended);
                        if (fileId != sector.FileID) throw new InvalidDataException("Invalid file id.");
                        if (indexId != sector.IndexID) throw new InvalidDataException("Invalid index id.");
                        if (currentChunkId != sector.ChunkID) throw new InvalidDataException("Invalid chunk id.");
                        if (sector.NextSectorID < 0 || (_dataFile.Length / (long)(Sector.DataSize)) < sector.NextSectorID)
                            throw new InvalidDataException("Invalid next sector id.");

                        for (var i = headerSectorSize; dataSectorSize + headerSectorSize > i; i++) data[readBytesCount++] = dataBuffer[i];

                        currentChunkId++;
                        currentSectorId = sector.NextSectorID;
                    }

                    return new MemoryStream(data);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(indexData);
                }
            }
        }

        /// <summary>
        /// Writes the file to the data and index store.
        /// </summary>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="fileId">The file identifier.</param>
        /// <param name="data">The data.</param>
        public void Write(int indexId, int fileId, MemoryStream data)
        {
            if (Write(indexId, fileId, data, true))
            {
                return;
            }

            data.Position = 0;
            Write(indexId, fileId, data, false);
        }

        /// <summary>
        /// Writes the file to the data and index store.
        /// </summary>
        /// <param name="indexId">The index identifier.</param>
        /// <param name="fileId">The file identifier.</param>
        /// <param name="data">The data.</param>
        /// <param name="overwrite">if set to <c>true</c> [overwrite].</param>
        /// <returns></returns>
        /// <exception cref="System.IO.FileNotFoundException"></exception>
        /// <exception cref="System.IO.IOException"></exception>
        /// <exception cref="InvalidDataException">Invalid sector id.</exception>
        private bool Write(int indexId, int fileId, MemoryStream data, bool overwrite)
        {
            if (indexId >= IndexFileCount && indexId != 255) throw new FileNotFoundException();
            if (data.Length < 0 || data.Length > Sector.MaxFileSize) return false;

            var indexStream = indexId == 255 ? _mainIndexFile : _indexFiles[indexId];

            lock (_lockObj)
            {
                var nextSectorId = 0;
                long ptr = fileId * Index.IndexSize;
                if (overwrite)
                {
                    if (ptr < 0) throw new IOException();
                    if (ptr >= indexStream.Length) return false;

                    byte[] indexData = new byte[Index.IndexSize];
                    indexStream.Seek(Index.IndexSize * fileId, SeekOrigin.Begin);
                    indexStream.Read(indexData, 0, Index.IndexSize);

                    nextSectorId = Index.Decode(indexData).SectorID;
                    if (nextSectorId <= 0 || nextSectorId > (_dataFile.Length / (long)Sector.DataSize)) return false;
                }
                else
                {
                    nextSectorId = (int)((_dataFile.Length + Sector.DataSize - 1) / Sector.DataSize);
                    if (nextSectorId == 0) nextSectorId = 1;
                }

                var index = new Index((int)(data.Length - data.Position), nextSectorId);
                var newIndexData = index.Encode();
                indexStream.Seek(ptr, SeekOrigin.Begin);
                indexStream.Write(newIndexData, 0, newIndexData.Length);
                indexStream.Flush(true);

                var extended = fileId > ushort.MaxValue;
                var dataBuffer = new byte[Sector.DataSize];
                int currentChunkId = 0, remainingBytesCount = index.Size;
                while (remainingBytesCount > 0)
                {
                    var currentSectorId = nextSectorId;
                    if (currentSectorId == 0) throw new InvalidDataException("Invalid sector id.");

                    ptr = Sector.DataSize * currentSectorId;
                    nextSectorId = 0;

                    if (overwrite)
                    {
                        _dataFile.Seek(ptr, SeekOrigin.Begin);
                        _dataFile.Read(dataBuffer, 0, dataBuffer.Length);

                        var sector = Sector.Decode(dataBuffer, extended);

                        if (sector.IndexID != indexId) return false;

                        if (sector.FileID != fileId) return false;

                        if (sector.ChunkID != currentChunkId) return false;

                        nextSectorId = sector.NextSectorID;
                        if (nextSectorId < 0 || nextSectorId > _dataFile.Length / Sector.DataSize) return false;
                    }

                    if (nextSectorId == 0)
                    {
                        overwrite = false;
                        nextSectorId = (int)((_dataFile.Length + Sector.DataSize - 1) / Sector.DataSize);
                        if (nextSectorId == 0) nextSectorId = 1;
                        if (nextSectorId == currentSectorId) nextSectorId++;
                    }

                    var dataBlock = new byte[extended ? Sector.ExtendedDataBlockSize : Sector.DataBlockSize];
                    if (remainingBytesCount < dataBlock.Length)
                    {
                        data.Read(dataBlock, 0, remainingBytesCount);
                        nextSectorId = 0; // mark as end of file
                        remainingBytesCount = 0;
                    }
                    else
                    {
                        remainingBytesCount -= dataBlock.Length;
                        data.Read(dataBlock, 0, dataBlock.Length);
                    }

                    var newSector = new Sector(fileId, currentChunkId++, nextSectorId, indexId);
                    byte[] newSectorData = newSector.Encode(dataBlock);
                    _dataFile.Seek(ptr, SeekOrigin.Begin);
                    _dataFile.Write(newSectorData, 0, newSectorData.Length);
                }

                _dataFile.Flush(true);
                return true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() => Dispose(true);

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        ///   <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            lock (_lockObj)
            {
                _dataFile.Dispose();
                _dataFile = null!;

                _mainIndexFile.Dispose();
                _mainIndexFile = null!;

                foreach (var file in _indexFiles)
                {
                    file.Dispose();
                }

                _indexFiles = null!;
            }
        }
    }
}