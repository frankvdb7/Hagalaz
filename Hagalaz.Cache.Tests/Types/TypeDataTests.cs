using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Types.Data;
using Xunit;

namespace Hagalaz.Cache.Tests.Types
{
    public class TypeDataTests
    {
        // Test class to verify the default implementation in TypeData
        private class DefaultTypeData : TypeData
        {
            public override byte IndexId => 1;
            public override int ArchiveEntryOffset => 8; // 2^8 = 256 entries per archive
            public override int ArchiveEntrySize => 1;
            public override int GetArchiveSize(ICacheAPI cache) => 0; // Not relevant for this test
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(255, 0, 255)]
        [InlineData(256, 1, 0)]
        [InlineData(511, 1, 255)]
        [InlineData(512, 2, 0)]
        public void DefaultTypeData_Calculates_Correct_Archive_And_Entry_Ids(int typeId, int expectedArchiveId, int expectedEntryId)
        {
            // Arrange
            var typeData = new DefaultTypeData();

            // Act
            var archiveId = typeData.GetArchiveId(typeId);
            var entryId = typeData.GetArchiveEntryId(typeId);

            // Assert
            Assert.Equal(expectedArchiveId, archiveId);
            Assert.Equal(expectedEntryId, entryId);
        }

        [Theory]
        [InlineData(0, 16, 0)]
        [InlineData(123, 16, 123)]
        [InlineData(999, 16, 999)]
        public void ConfigDefinitionData_Always_Uses_Archive16(int typeId, int expectedArchiveId, int expectedEntryId)
        {
            // Arrange
            var configData = new ConfigDefinitionData();

            // Act
            var archiveId = configData.GetArchiveId(typeId);
            var entryId = configData.GetArchiveEntryId(typeId);

            // Assert
            Assert.Equal(expectedArchiveId, archiveId);
            Assert.Equal(expectedEntryId, entryId);
        }
    }
}
