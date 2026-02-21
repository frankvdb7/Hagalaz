using System.Text;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Xunit;
using Index = Hagalaz.Cache.Models.Index;

namespace Hagalaz.Cache.Tests
{
    public class FileStoreTests : IDisposable
    {
        private readonly IIndexCodec _indexCodec;
        private readonly ISectorCodec _sectorCodec;
        private readonly FileStream _dataFile;
        private readonly FileStream[] _indexFiles;
        private readonly FileStream _mainIndexFile;
        private readonly FileStore _fileStore;

        public FileStoreTests()
        {
            _indexCodec = new IndexCodec();
            _sectorCodec = new SectorCodec();

            _dataFile = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose);
            _indexFiles = new[]
            {
                new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose)
            };
            _mainIndexFile = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose);

            _fileStore = new FileStore(_dataFile, _indexFiles, _mainIndexFile, _indexCodec, _sectorCodec);
        }

        [Fact]
        public void Read_ValidFile_ReturnsCorrectData()
        {
            // Arrange
            const int indexId = 0;
            const int fileId = 1;
            var fileData = Encoding.UTF8.GetBytes("This is a test file.");

            _fileStore.Write(indexId, fileId, new MemoryStream(fileData));

            // Act
            var resultStream = _fileStore.Read(indexId, fileId);

            // Assert
            var resultData = resultStream.ToArray();
            Assert.Equal(fileData, resultData);
        }

        [Fact]
        public void Read_InvalidIndexId_ThrowsFileNotFoundException()
        {
            // Arrange
            const int invalidIndexId = 99;
            const int fileId = 1;

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => _fileStore.Read(invalidIndexId, fileId));
        }

        [Fact]
        public void Read_InvalidFileId_ThrowsFileNotFoundException()
        {
            // Arrange
            const int indexId = 0;
            const int invalidFileId = 99;

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => _fileStore.Read(indexId, invalidFileId));
        }

        [Fact]
        public void Write_And_Read_Multiple_Files()
        {
            // Arrange
            var files = new (int indexId, int fileId, byte[] data)[]
            {
                (0, 1, Encoding.UTF8.GetBytes("File 1 content")),
                (0, 2, Encoding.UTF8.GetBytes("File 2 content with a bit more data to test multiple sectors")),
                (0, 3, new byte[Sector.DataBlockSize + 10]) // Larger than one sector
            };
            new Random().NextBytes(files[2].data);

            // Act
            foreach (var file in files)
            {
                _fileStore.Write(file.indexId, file.fileId, new MemoryStream(file.data));
            }

            // Assert
            foreach (var file in files)
            {
                var resultStream = _fileStore.Read(file.indexId, file.fileId);
                var resultData = resultStream.ToArray();
                Assert.Equal(file.data, resultData);
            }
        }

        [Fact]
        public void GetFileCount_ReturnsCorrectCount()
        {
            // Arrange
            const int indexId = 0;
            const int fileCount = 5;
            for (var i = 0; i < fileCount; i++)
            {
                var fileData = Encoding.UTF8.GetBytes($"File content {i}");
                _fileStore.Write(indexId, i, new MemoryStream(fileData));
            }

            // Act
            var result = _fileStore.GetFileCount(indexId);

            // Assert
            Assert.Equal(fileCount, result);
        }

        [Fact]
        public void Write_And_Read_MainIndexFile_255()
        {
            // Arrange
            const int indexId = 255;
            const int fileId = 5;
            var fileData = Encoding.UTF8.GetBytes("Main index file test data.");

            // Act
            _fileStore.Write(indexId, fileId, new MemoryStream(fileData));
            var resultStream = _fileStore.Read(indexId, fileId);
            var fileCount = _fileStore.GetFileCount(indexId);

            // Assert
            var resultData = resultStream.ToArray();
            Assert.Equal(fileData, resultData);
            Assert.True(fileCount > fileId);
        }

        [Fact]
        public void Write_Overwrite_UpdatesExistingFile()
        {
            // Arrange
            const int indexId = 0;
            const int fileId = 1;
            var initialData = Encoding.UTF8.GetBytes("Initial data.");
            var updatedData = Encoding.UTF8.GetBytes("This is the updated data which is longer.");

            // Act
            _fileStore.Write(indexId, fileId, new MemoryStream(initialData));
            var initialRead = _fileStore.Read(indexId, fileId).ToArray();

            _fileStore.Write(indexId, fileId, new MemoryStream(updatedData));
            var updatedRead = _fileStore.Read(indexId, fileId).ToArray();

            // Assert
            Assert.Equal(initialData, initialRead);
            Assert.Equal(updatedData, updatedRead);
        }

        [Fact]
        public void Read_CorruptedIndex_InvalidSize_ThrowsInvalidDataException()
        {
            // Arrange
            const int indexId = 0;
            const int fileId = 1;

            var indexStream = _indexFiles[indexId];
            var corruptedIndex = new Index(-1, 1); // Invalid size
            var corruptedIndexData = _indexCodec.Encode(corruptedIndex);
            indexStream.Seek(Index.IndexSize * fileId, SeekOrigin.Begin);
            indexStream.Write(corruptedIndexData);
            indexStream.Flush();

            // Act & Assert
            var ex = Assert.Throws<InvalidDataException>(() => _fileStore.Read(indexId, fileId));
            Assert.Equal("Index size is invalid.", ex.Message);
        }

        [Fact]
        public void Read_CorruptedIndex_InvalidSector_ThrowsInvalidDataException()
        {
            // Arrange
            const int indexId = 0;
            const int fileId = 1;

            var indexStream = _indexFiles[indexId];
            var corruptedIndex = new Index(10, 9999); // Invalid sector ID
            var corruptedIndexData = _indexCodec.Encode(corruptedIndex);
            indexStream.Seek(Index.IndexSize * fileId, SeekOrigin.Begin);
            indexStream.Write(corruptedIndexData);
            indexStream.Flush();

            // Act & Assert
            var ex = Assert.Throws<InvalidDataException>(() => _fileStore.Read(indexId, fileId));
            Assert.Equal("Index sector id is invalid.", ex.Message);
        }

        [Fact]
        public void Read_CorruptedSector_InvalidFileId_ThrowsInvalidDataException()
        {
            // Arrange
            const int indexId = 0;
            const int fileId = 1;
            const int wrongFileId = 2;
            var fileData = Encoding.UTF8.GetBytes("test");

            _fileStore.Write(indexId, fileId, new MemoryStream(fileData));

            // Get the sector ID from the index
            _indexFiles[0].Seek(fileId * Index.IndexSize, SeekOrigin.Begin);
            var indexBytes = new byte[Index.IndexSize];
            _indexFiles[0].Read(indexBytes);
            var index = _indexCodec.Decode(indexBytes);

            // Read the original sector
            _dataFile.Seek(index.SectorID * Sector.DataSize, SeekOrigin.Begin);
            var sectorBytes = new byte[Sector.DataSize];
            _dataFile.Read(sectorBytes);
            var sector = _sectorCodec.Decode(sectorBytes, false);

            // Create and write corrupted sector
            var corruptedSector = new Sector(wrongFileId, sector.ChunkID, sector.NextSectorID, sector.IndexID);
            var dataBlock = sectorBytes.Skip(Sector.DataHeaderSize).ToArray();
            var corruptedSectorData = _sectorCodec.Encode(corruptedSector, dataBlock);

            _dataFile.Seek(index.SectorID * Sector.DataSize, SeekOrigin.Begin);
            _dataFile.Write(corruptedSectorData);
            _dataFile.Flush();

            // Act & Assert
            var ex = Assert.Throws<InvalidDataException>(() => _fileStore.Read(indexId, fileId));
            Assert.Equal("Invalid file id.", ex.Message);
        }

        public void Dispose()
        {
            _fileStore.Dispose();
        }
        [Fact]
        public void Write_And_Read_LargeFile_Succeeds()
        {
            // Arrange
            const int indexId = 0;
            const int fileId = 10;
            var fileData = new byte[10000];
            new Random().NextBytes(fileData);

            // Act
            _fileStore.Write(indexId, fileId, new MemoryStream(fileData));
            var resultStream = _fileStore.Read(indexId, fileId);

            // Assert
            var resultData = resultStream.ToArray();
            Assert.Equal(fileData, resultData);
        }


        [Fact]
        public void Write_And_Read_ExtendedFile_Succeeds()
        {
            // Arrange
            const int indexId = 0;
            const int fileId = ushort.MaxValue + 1;
            var fileData = System.Text.Encoding.UTF8.GetBytes("This is a test file for extended file IDs.");

            // Act
            _fileStore.Write(indexId, fileId, new MemoryStream(fileData));
            var resultStream = _fileStore.Read(indexId, fileId);

            // Assert
            var resultData = resultStream.ToArray();
            Assert.Equal(fileData, resultData);
        }
}
}