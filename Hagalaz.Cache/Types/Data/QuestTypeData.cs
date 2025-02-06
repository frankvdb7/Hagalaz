namespace Hagalaz.Cache.Types.Data
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="TypeData" />
    public class QuestTypeData : TypeData
    {
        /// <summary>
        /// The index identifier
        /// </summary>
        public override byte IndexId => 2;
        /// <summary>
        /// The archive entry offset
        /// </summary>
        public override int ArchiveEntryOffset => 35;
        /// <summary>
        /// Gets the size of the archive entry.
        /// </summary>
        /// <value>
        /// The size of the archive entry.
        /// </value>
        public override int ArchiveEntrySize => 0;
    }
}
