namespace Hagalaz.Cache.Types.Data
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Hagalaz.Cache.Types.TypeData" />
    public class ItemTypeData : TypeData
    {
        /// <summary>
        /// The index identifier
        /// </summary>
        public override byte IndexId => 19;
        /// <summary>
        /// The archive entry offset
        /// </summary>
        public override int ArchiveEntryOffset => 8;
        /// <summary>
        /// Gets the size of the archive entry.
        /// </summary>
        /// <value>
        /// The size of the archive entry.
        /// </value>
        public override int ArchiveEntrySize => 1 << ArchiveEntryOffset;
    }
}
