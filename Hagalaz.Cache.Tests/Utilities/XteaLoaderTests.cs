using Hagalaz.Cache.Utilities;
using Xunit;
using System.IO;
using System.Collections.Generic;

namespace Hagalaz.Cache.Tests.Utilities
{
    public class XteaLoaderTests : System.IDisposable
    {
        private const string DataDirectory = "./data";
        private const string XteaDirectory = "./data/xtea";
        private const string PackedFile = "./data/xtea/packed.bin";

        public XteaLoaderTests()
        {
            // Ensure the test environment is clean before each test
            if (Directory.Exists(DataDirectory))
            {
                Directory.Delete(DataDirectory, true);
            }
        }

        public void Dispose()
        {
            // Cleanup after each test
            if (Directory.Exists(DataDirectory))
            {
                Directory.Delete(DataDirectory, true);
            }
        }

        [Fact]
        public void Load_ValidPackedFile_LoadsXteaKeys()
        {
            // Arrange
            Directory.CreateDirectory(XteaDirectory);
            var regions = new Dictionary<int, int[]>();
            using (var writer = new BinaryWriter(File.Open(PackedFile, FileMode.Create)))
            {
                // Region 1
                writer.Write(12345);
                writer.Write(1);
                writer.Write(2);
                writer.Write(3);
                writer.Write(4);
                // Region 2
                writer.Write(54321);
                writer.Write(5);
                writer.Write(6);
                writer.Write(7);
                writer.Write(8);
            }

            // Act
            XteaLoader.Load(regions);

            // Assert
            Assert.Equal(2, regions.Count);
            Assert.True(regions.ContainsKey(12345));
            Assert.Equal(new int[] { 1, 2, 3, 4 }, regions[12345]);
            Assert.True(regions.ContainsKey(54321));
            Assert.Equal(new int[] { 5, 6, 7, 8 }, regions[54321]);
        }

        [Fact]
        public void Load_PackedFileMissing_CallsPackerAndLoadsNothing()
        {
            // Arrange
            Directory.CreateDirectory(XteaDirectory);
            var regions = new Dictionary<int, int[]>();

            // Act
            XteaLoader.Load(regions);

            // Assert
            Assert.Empty(regions);
            Assert.True(File.Exists(PackedFile)); // XteaPacker.Pack() creates the file.
        }

        [Fact]
        public void Load_EmptyPackedFile_LoadsNothing()
        {
            // Arrange
            Directory.CreateDirectory(XteaDirectory);
            var regions = new Dictionary<int, int[]>();
            File.Create(PackedFile).Close();

            // Act
            XteaLoader.Load(regions);

            // Assert
            Assert.Empty(regions);
        }

        [Fact]
        public void Load_CorruptedPackedFile_HandlesExceptionAndLoadsNothing()
        {
            // Arrange
            Directory.CreateDirectory(XteaDirectory);
            var regions = new Dictionary<int, int[]>();
            using (var writer = new BinaryWriter(File.Open(PackedFile, FileMode.Create)))
            {
                writer.Write(12345); // Partial record
            }

            // Act
            XteaLoader.Load(regions);

            // Assert
            Assert.Empty(regions);
        }
    }
}