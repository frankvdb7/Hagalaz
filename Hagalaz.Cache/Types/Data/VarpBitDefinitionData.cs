namespace Hagalaz.Cache.Types.Data
{
    /// <summary>
    /// Contains information about where the VarpBit definitions are stored in the cache.
    /// </summary>
    public class VarpBitDefinitionData : TypeData
    {
        /// <inheritdoc />
        public override byte IndexId => 22;

        /// <inheritdoc />
        public override int ArchiveEntryOffset => 10;

        /// <inheritdoc />
        public override int ArchiveEntrySize => 1 << ArchiveEntryOffset;

        /// <inheritdoc />
        public override int GetArchiveId(int typeId) => typeId >> ArchiveEntryOffset;

        /// <inheritdoc />
        public override int GetArchiveEntryId(int typeId) => typeId & (ArchiveEntrySize - 1);
    }
}
