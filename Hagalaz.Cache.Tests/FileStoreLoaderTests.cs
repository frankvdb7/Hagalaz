using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class FileStoreLoaderTests
    {
        private readonly string _tempPath;
        private readonly Mock<ILogger<FileStore>> _loggerMock;
        private readonly Mock<IIndexCodec> _indexCodecMock;
        private readonly Mock<ISectorCodec> _sectorCodecMock;
        private readonly FileStoreLoader _fileStoreLoader;

        public FileStoreLoaderTests()
        {
            _tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempPath);

            _loggerMock = new Mock<ILogger<FileStore>>();
            _indexCodecMock = new Mock<IIndexCodec>();
            _sectorCodecMock = new Mock<ISectorCodec>();
            _fileStoreLoader = new FileStoreLoader(_loggerMock.Object, _indexCodecMock.Object, _sectorCodecMock.Object);
        }

        private void CreateCacheFiles(bool createDataFile = true, int indexFileCount = 1, bool createMainIndexFile = true)
        {
            if (createDataFile)
            {
                File.Create(Path.Combine(_tempPath, "main_file_cache.dat2")).Close();
            }

            for (int i = 0; i < indexFileCount; i++)
            {
                File.Create(Path.Combine(_tempPath, $"main_file_cache.idx{i}")).Close();
            }

            if (createMainIndexFile)
            {
                File.Create(Path.Combine(_tempPath, "main_file_cache.idx255")).Close();
            }
        }

        [Fact]
        public void Open_WhenFilesExist_ReturnsFileStore()
        {
            // Arrange
            CreateCacheFiles();

            // Act
            var fileStore = _fileStoreLoader.Open(_tempPath);

            // Assert
            Assert.NotNull(fileStore);

            // Cleanup
            Directory.Delete(_tempPath, true);
        }

        [Fact]
        public void Open_WhenDataFileMissing_ThrowsFileNotFoundException()
        {
            // Arrange
            CreateCacheFiles(createDataFile: false);

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => _fileStoreLoader.Open(_tempPath));

            // Cleanup
            Directory.Delete(_tempPath, true);
        }

        [Fact]
        public void Open_WhenNoIndexFiles_ThrowsFileNotFoundException()
        {
            // Arrange
            CreateCacheFiles(indexFileCount: 0);

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => _fileStoreLoader.Open(_tempPath));

            // Cleanup
            Directory.Delete(_tempPath, true);
        }

        [Fact]
        public void Open_WhenMainIndexFileMissing_ThrowsFileNotFoundException()
        {
            // Arrange
            CreateCacheFiles(createMainIndexFile: false);

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => _fileStoreLoader.Open(_tempPath));

            // Cleanup
            Directory.Delete(_tempPath, true);
        }
    }
}