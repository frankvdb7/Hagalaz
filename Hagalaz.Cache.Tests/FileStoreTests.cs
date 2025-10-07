using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class FileStoreTests : IDisposable
    {
        private readonly IIndexCodec _indexCodec;
        private readonly ISectorCodec _sectorCodec;
        private readonly List<FileStream> _tempStreams = new List<FileStream>();
        private readonly List<string> _tempFilePaths = new List<string>();

        public FileStoreTests()
        {
            _indexCodec = new IndexCodec();
            _sectorCodec = new SectorCodec();
        }

        private FileStore CreateFileStore(MemoryStream dataStream, MemoryStream[] indexStreams, MemoryStream mainIndexStream)
        {
            var dataFilePath = Path.GetTempFileName();
            var dataFileStream = new FileStream(dataFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            dataStream.Position = 0;
            dataStream.CopyTo(dataFileStream);
            dataFileStream.Position = 0;
            _tempStreams.Add(dataFileStream);
            _tempFilePaths.Add(dataFilePath);

            var indexFileStreams = new FileStream[indexStreams.Length];
            for (int i = 0; i < indexStreams.Length; i++)
            {
                var indexFilePath = Path.GetTempFileName();
                indexFileStreams[i] = new FileStream(indexFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                indexStreams[i].Position = 0;
                indexStreams[i].CopyTo(indexFileStreams[i]);
                indexFileStreams[i].Position = 0;
                _tempStreams.Add(indexFileStreams[i]);
                _tempFilePaths.Add(indexFilePath);
            }

            var mainIndexFilePath = Path.GetTempFileName();
            var mainIndexFileStream = new FileStream(mainIndexFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            mainIndexStream.Position = 0;
            mainIndexStream.CopyTo(mainIndexFileStream);
            mainIndexFileStream.Position = 0;
            _tempStreams.Add(mainIndexFileStream);
            _tempFilePaths.Add(mainIndexFilePath);

            return new FileStore(dataFileStream, indexFileStreams, mainIndexFileStream, _indexCodec, _sectorCodec);
        }

        [Fact]
        public void Read_SingleSectorFile_ReturnsCorrectData()
        {
            // Arrange
            var dataStream = new MemoryStream();
            var indexStream = new MemoryStream();
            var mainIndexStream = new MemoryStream();
            var fileStore = CreateFileStore(dataStream, new[] { indexStream }, mainIndexStream);

            var fileContent = "Hello, World!";
            var fileBytes = Encoding.UTF8.GetBytes(fileContent);
            var fileDataStream = new MemoryStream(fileBytes);

            // Act
            fileStore.Write(0, 0, fileDataStream);
            var resultStream = fileStore.Read(0, 0);
            var resultBytes = resultStream.ToArray();
            var resultString = Encoding.UTF8.GetString(resultBytes);

            // Assert
            Assert.Equal(fileContent, resultString);
        }

        [Fact]
        public void Read_MultiSectorFile_ReturnsCorrectData()
        {
            // Arrange
            var dataStream = new MemoryStream();
            var indexStream = new MemoryStream();
            var mainIndexStream = new MemoryStream();
            var fileStore = CreateFileStore(dataStream, new[] { indexStream }, mainIndexStream);

            var fileContent = new string('a', 600);
            var fileBytes = Encoding.UTF8.GetBytes(fileContent);
            var fileDataStream = new MemoryStream(fileBytes);

            // Act
            fileStore.Write(0, 0, fileDataStream);
            var resultStream = fileStore.Read(0, 0);
            var resultBytes = resultStream.ToArray();
            var resultString = Encoding.UTF8.GetString(resultBytes);

            // Assert
            Assert.Equal(fileContent, resultString);
        }

        [Fact]
        public void Write_NewFile_ThenRead_ReturnsCorrectData()
        {
            // Arrange
            var dataStream = new MemoryStream();
            var indexStream = new MemoryStream();
            var mainIndexStream = new MemoryStream();
            var fileStore = CreateFileStore(dataStream, new[] { indexStream }, mainIndexStream);

            var fileContent = "This is a new file.";
            var fileBytes = Encoding.UTF8.GetBytes(fileContent);
            var fileDataStream = new MemoryStream(fileBytes);

            // Act
            fileStore.Write(0, 0, fileDataStream);

            // Assert
            var resultStream = fileStore.Read(0, 0);
            var resultBytes = resultStream.ToArray();
            var resultString = Encoding.UTF8.GetString(resultBytes);
            Assert.Equal(fileContent, resultString);
        }

        [Fact]
        public void Read_NonExistentFile_ThrowsFileNotFoundException()
        {
            // Arrange
            var dataStream = new MemoryStream();
            var indexStream = new MemoryStream();
            var mainIndexStream = new MemoryStream();
            var fileStore = CreateFileStore(dataStream, new[] { indexStream }, mainIndexStream);

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => fileStore.Read(0, 10));
        }

        public void Dispose()
        {
            foreach (var stream in _tempStreams)
            {
                stream.Dispose();
            }
            foreach (var path in _tempFilePaths)
            {
                try
                {
                    File.Delete(path);
                }
                catch (IOException)
                {
                    // Ignore errors during cleanup
                }
            }
        }
    }
}