using System.Buffers;
using System.IO;
using System.Threading;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Models;

namespace Hagalaz.Cache
{
    /// <summary>
    /// A file store holds multiple files inside a "virtual" file system made up of
    /// several index files and a single data file.
    /// </summary>
    public class FileStore : IFileStore
    {
        /// <summary>
        /// A lock object to ensure thread-safe access to the file streams.
        /// </summary>
        private readonly Lock _lockObj = new();

        /// <summary>
        /// The stream for the main data file (`main_file_cache.dat2`).
        /// </summary>
        private FileStream _dataFile;

        /// <summary>
        /// An array of streams for the index files (`main_file_cache.idx*`).
        /// </summary>
        private FileStream[] _indexFiles;

        /// <summary>
        /// The stream for the master index file (`main_file_cache.idx255`).
        /// </summary>
        private FileStream _mainIndexFile;

        private readonly IIndexCodec _indexCodec;
        private readonly ISectorCodec _sectorCodec;

        /// <summary>
        /// Gets the number of indices in this file store.
        /// </summary>
        public int IndexFileCount => _indexFiles.Length;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileStore" /> class.
        /// </summary>
        /// <param name="dataFile">The file stream for the main data file.</param>
        /// <param name="indexFiles">An array of file streams for the index files.</param>
        /// <param name="mainIndexFile">The file stream for the master index file (idx255).</param>
        /// <param name="indexCodec">The codec for reading and writing index data.</param>
        /// <param name="sectorCodec">The codec for reading and writing sector data.</param>
        public FileStore(FileStream dataFile, FileStream[] indexFiles, FileStream mainIndexFile, IIndexCodec indexCodec, ISectorCodec sectorCodec)
        {
            _dataFile = dataFile;
            _indexFiles = indexFiles;
            _mainIndexFile = mainIndexFile;
            _indexCodec = indexCodec;
            _sectorCodec = sectorCodec;
        }

        /// <summary>
        /// Gets the total number of files within a specified index.
        /// </summary>
        /// <param name="indexId">The identifier of the index.</param>
        /// <returns>The number of files in the index.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the specified <paramref name="indexId"/> is invalid.</exception>
        public int GetFileCount(int indexId)
        {
            if (indexId >= IndexFileCount && indexId != 255) throw new FileNotFoundException();
            if (indexId == 255) return (int)(_mainIndexFile.Length / Models.Index.IndexSize);
            return (int)(_indexFiles[indexId].Length / Models.Index.IndexSize);
        }

        /// <summary>
        /// Reads a file from the cache, following the chain of sectors in the data file.
        /// </summary>
        /// <param name="indexId">The identifier of the index (cache) from which to read.</param>
        /// <param name="fileId">The identifier of the file to read.</param>
        /// <returns>A <see cref="MemoryStream"/> containing the raw, decompressed data of the file.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the specified index or file does not exist.</exception>
        /// <exception cref="InvalidDataException">Thrown if the cache data is corrupt or inconsistent (e.g., invalid sector chains, mismatched IDs).</exception>
        public MemoryStream Read(int indexId, int fileId)
        {
            if (indexId >= IndexFileCount && indexId != 255) throw new FileNotFoundException();

            var indexStream = indexId == 255 ? _mainIndexFile : _indexFiles[indexId];

            if (((long)Models.Index.IndexSize * fileId + Models.Index.IndexSize) > indexStream.Length) throw new FileNotFoundException();

            lock (_lockObj)
            {
                var indexData = ArrayPool<byte>.Shared.Rent(Models.Index.IndexSize);
                try
                {
                    indexStream.Seek((long)Models.Index.IndexSize * fileId, SeekOrigin.Begin);
                    indexStream.ReadExactly(indexData, 0, Models.Index.IndexSize);

                    var index = _indexCodec.Decode(indexData);
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
                        _dataFile.Seek((long)Sector.DataSize * currentSectorId, SeekOrigin.Begin);
                        var dataSectorSize = index.Size - readBytesCount;
                        if (dataSectorSize > sectorSize) dataSectorSize = sectorSize;
                        _dataFile.ReadExactly(dataBuffer, 0, headerSectorSize + dataSectorSize);

                        var sector = _sectorCodec.Decode(dataBuffer, extended);
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
                long ptr = (long)fileId * Models.Index.IndexSize;
                if (overwrite)
                {
                    if (ptr < 0) throw new IOException();
                    if (ptr >= indexStream.Length) return false;

                    byte[] indexData = new byte[Models.Index.IndexSize];
                    indexStream.Seek((long)Models.Index.IndexSize * fileId, SeekOrigin.Begin);
                    indexStream.ReadExactly(indexData, 0, Models.Index.IndexSize);

                    nextSectorId = _indexCodec.Decode(indexData).SectorID;
                    if (nextSectorId <= 0 || nextSectorId > (_dataFile.Length / (long)Sector.DataSize)) return false;
                }
                else
                {
                    nextSectorId = (int)((_dataFile.Length + Sector.DataSize - 1) / Sector.DataSize);
                    if (nextSectorId == 0) nextSectorId = 1;
                }

                var index = new Index((int)(data.Length - data.Position), nextSectorId);
                var newIndexData = _indexCodec.Encode(index);
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

                    ptr = (long)Sector.DataSize * currentSectorId;
                    nextSectorId = 0;

                    if (overwrite)
                    {
                        _dataFile.Seek(ptr, SeekOrigin.Begin);
                        _dataFile.ReadExactly(dataBuffer);

                        var sector = _sectorCodec.Decode(dataBuffer, extended);

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
                    byte[] newSectorData = _sectorCodec.Encode(newSector, dataBlock);
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