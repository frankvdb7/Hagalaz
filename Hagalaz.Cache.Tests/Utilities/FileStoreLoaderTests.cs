using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.IO;

namespace Hagalaz.Cache.Tests.Utilities
{
    public class FileStoreLoaderTests
    {
        private readonly Mock<ILogger<FileStore>> _loggerMock;
        private readonly Mock<IIndexCodec> _indexCodecMock;
        private readonly Mock<ISectorCodec> _sectorCodecMock;
        private readonly FileStoreLoader _loader;

        public FileStoreLoaderTests()
        {
            _loggerMock = new Mock<ILogger<FileStore>>();
            _indexCodecMock = new Mock<IIndexCodec>();
            _sectorCodecMock = new Mock<ISectorCodec>();
            _loader = new FileStoreLoader(_loggerMock.Object, _indexCodecMock.Object, _sectorCodecMock.Object);
        }

        [Fact]
        public void Open_ValidData_ReturnsFileStore()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            File.WriteAllBytes(Path.Combine(tempDir, "main_file_cache.dat2"), new byte[1]);
            File.WriteAllBytes(Path.Combine(tempDir, "main_file_cache.idx0"), new byte[1]);
            File.WriteAllBytes(Path.Combine(tempDir, "main_file_cache.idx255"), new byte[1]);

            // Act
            var fileStore = _loader.Open(tempDir);

            // Assert
            Assert.NotNull(fileStore);
            Assert.IsAssignableFrom<IFileStore>(fileStore);

            // Cleanup
            fileStore.Dispose(); // Dispose the filestore to release file locks
            Directory.Delete(tempDir, true);
        }

        [Fact]
        public void Open_DataFileNotFound_ThrowsFileNotFoundException()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            File.WriteAllBytes(Path.Combine(tempDir, "main_file_cache.idx0"), new byte[1]);
            File.WriteAllBytes(Path.Combine(tempDir, "main_file_cache.idx255"), new byte[1]);

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => _loader.Open(tempDir));

            // Cleanup
            Directory.Delete(tempDir, true);
        }

        [Fact]
        public void Open_IndexFilesNotFound_ThrowsFileNotFoundException()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            File.WriteAllBytes(Path.Combine(tempDir, "main_file_cache.dat2"), new byte[1]);
            File.WriteAllBytes(Path.Combine(tempDir, "main_file_cache.idx255"), new byte[1]);

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => _loader.Open(tempDir));

            // Cleanup
            Directory.Delete(tempDir, true);
        }

        [Fact]
        public void Open_MainIndexFileNotFound_ThrowsFileNotFoundException()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            File.WriteAllBytes(Path.Combine(tempDir, "main_file_cache.dat2"), new byte[1]);
            File.WriteAllBytes(Path.Combine(tempDir, "main_file_cache.idx0"), new byte[1]);

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => _loader.Open(tempDir));

            // Cleanup
            Directory.Delete(tempDir, true);
        }

        [Fact]
        public void Open_DirectoryNotFound_ThrowsDirectoryNotFoundException()
        {
            // Arrange
            string nonExistentDir = Path.Combine(Path.GetTempPath(), "non_existent_dir_for_testing");

            // Act & Assert
            Assert.Throws<DirectoryNotFoundException>(() => _loader.Open(nonExistentDir));
        }
    }
}