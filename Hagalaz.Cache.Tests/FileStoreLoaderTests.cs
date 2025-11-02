using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Hagalaz.Cache.Tests
{
    public class FileStoreLoaderTests : IAsyncLifetime
    {
        private string _tempPath;
        private Mock<ILogger<FileStore>> _loggerMock;
        private Mock<IIndexCodec> _indexCodecMock;
        private Mock<ISectorCodec> _sectorCodecMock;
        private FileStoreLoader _fileStoreLoader;

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


        public Task InitializeAsync()
        {
            _tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempPath);

            _loggerMock = new Mock<ILogger<FileStore>>();
            _indexCodecMock = new Mock<IIndexCodec>();
            _sectorCodecMock = new Mock<ISectorCodec>();
            _fileStoreLoader = new FileStoreLoader(_loggerMock.Object, _indexCodecMock.Object, _sectorCodecMock.Object);

            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            // Cleanup
            Directory.Delete(_tempPath, true);
            return Task.CompletedTask;
        }

        [Fact]
        public void Open_WhenFilesExist_ReturnsFileStore()
        {
            // Arrange
            CreateCacheFiles();

            // Act
            using var fileStore = _fileStoreLoader.Open(_tempPath);

            // Assert
            Assert.NotNull(fileStore);
        }

        [Fact]
        public void Open_WhenDataFileMissing_ThrowsFileNotFoundException()
        {
            // Arrange
            CreateCacheFiles(createDataFile: false);

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => _fileStoreLoader.Open(_tempPath));
        }

        [Fact]
        public void Open_WhenNoIndexFiles_ThrowsFileNotFoundException()
        {
            // Arrange
            CreateCacheFiles(indexFileCount: 0);

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() =>
            {
                using var fileStore = _fileStoreLoader.Open(_tempPath);
            });

        }

        [Fact]
        public void Open_WhenMainIndexFileMissing_ThrowsFileNotFoundException()
        {
            // Arrange
            CreateCacheFiles(createMainIndexFile: false);

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() =>
            {
                using var fileStore = _fileStoreLoader.Open(_tempPath);
            });
        }
    }
}