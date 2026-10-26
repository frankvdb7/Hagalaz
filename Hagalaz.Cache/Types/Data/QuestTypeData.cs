using Hagalaz.Cache.Abstractions;

namespace Hagalaz.Cache.Types.Data
{
    /// <summary>Provides the cache location for quest definitions.</summary>
    public class QuestTypeData : TypeData
    {
        private const int QuestArchiveId = 35;

        /// <inheritdoc />
        public override byte IndexId => 2;

        /// <inheritdoc />
        public override int ArchiveEntryOffset => 0;

        /// <inheritdoc />
        public override int ArchiveEntrySize => 1;

        /// <inheritdoc />
        public override int GetArchiveSize(ICacheAPI cache) =>
            GetArchiveEntryIdUpperBound(cache.ReadReferenceTable(IndexId).GetEntry(QuestArchiveId));

        /// <inheritdoc />
        public override int GetArchiveId(int typeId) => QuestArchiveId;

        /// <inheritdoc />
        public override int GetArchiveEntryId(int typeId) => typeId;
    }
}
