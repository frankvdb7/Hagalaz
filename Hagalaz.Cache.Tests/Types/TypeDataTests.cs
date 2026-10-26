using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Types.Data;
using Moq;
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

        [Fact]
        public void ConfigDefinitionData_GetArchiveSize_WithSparseDefinitions_ReturnsHighestMappedIdPlusOne()
        {
            Assert.Equal(6, GetArchiveSize(new ConfigDefinitionData(), ArchiveGroup(16, 0, 5)));
        }

        [Theory]
        [InlineData(0, 35, 0)]
        [InlineData(35, 35, 35)]
        [InlineData(207, 35, 207)]
        public void QuestTypeData_UsesFixedArchive35(int typeId, int expectedArchiveId, int expectedEntryId)
        {
            var typeData = new QuestTypeData();

            Assert.Equal(expectedArchiveId, typeData.GetArchiveId(typeId));
            Assert.Equal(expectedEntryId, typeData.GetArchiveEntryId(typeId));
        }

        [Fact]
        public void QuestTypeData_GetArchiveSize_IgnoresUnrelatedIndex2Archives()
        {
            var unrelatedConfigArchive = Enumerable.Range(0, 256).ToArray();

            Assert.Equal(3, GetArchiveSize(
                new QuestTypeData(),
                ArchiveGroup(35, 0, 2),
                ArchiveGroup(16, unrelatedConfigArchive)));
        }

        [Fact]
        public void ObjectTypeData_GetArchiveSize_WithEmptyReferenceTable_ReturnsZero()
        {
            Assert.Equal(0, GetObjectArchiveSize());
        }

        [Fact]
        public void ObjectTypeData_GetArchiveSize_WithSingleArchive_ReturnsHighestMappedIdPlusOne()
        {
            Assert.Equal(2, GetObjectArchiveSize(ArchiveGroup(0, 0, 1)));
        }

        [Fact]
        public void ObjectTypeData_GetArchiveSize_WithFullArchives_ReturnsExclusiveUpperBound()
        {
            var fullArchive = Enumerable.Range(0, 256).ToArray();

            Assert.Equal(512, GetObjectArchiveSize(
                (0, fullArchive),
                (1, fullArchive)));
        }

        [Fact]
        public void ObjectTypeData_GetArchiveSize_WithPartialFinalArchive_ReturnsHighestMappedIdPlusOne()
        {
            var fullArchive = Enumerable.Range(0, 256).ToArray();

            Assert.Equal(259, GetObjectArchiveSize(
                (0, fullArchive),
                ArchiveGroup(1, 0, 1, 2)));
        }

        [Fact]
        public void ObjectTypeData_GetArchiveSize_WithSparseFinalGroups_IncludesIdentifierGaps()
        {
            var fullArchive = Enumerable.Range(0, 256).ToArray();

            Assert.Equal(515, GetObjectArchiveSize(
                (0, fullArchive),
                ArchiveGroup(2, 0, 2)));
        }

        private static int GetObjectArchiveSize(params (int ArchiveId, int[] ChildIds)[] groups)
            => GetArchiveSize(new ObjectTypeData(), groups);

        private static int GetArchiveSize(TypeData typeData, params (int ArchiveId, int[] ChildIds)[] groups)
        {
            var table = new ReferenceTable(7, 1, (ReferenceTableFlags)0, groups.Length);
            foreach (var (archiveId, childIds) in groups)
            {
                var archive = new ReferenceTableEntry(archiveId);
                archive.InitializeEntries(childIds.Length);
                for (var index = 0; index < childIds.Length; index++)
                {
                    archive.AddEntry(childIds[index], new ReferenceTableChildEntry(index));
                }

                table.AddEntry(archiveId, archive);
            }

            var cache = new Mock<ICacheAPI>();
            cache.Setup(current => current.ReadReferenceTable(typeData.IndexId)).Returns(table);
            return typeData.GetArchiveSize(cache.Object);
        }

        private static (int ArchiveId, int[] ChildIds) ArchiveGroup(int archiveId, params int[] childIds) =>
            (archiveId, childIds);
    }
}
