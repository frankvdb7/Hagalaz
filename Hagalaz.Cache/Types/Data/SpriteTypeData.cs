namespace Hagalaz.Cache.Types.Data
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Hagalaz.Cache.Types.TypeData" />
    public class SpriteTypeData : TypeData
    {
        /// <summary>
        /// Gets the index identifier.
        /// </summary>
        /// <value>
        /// The index identifier.
        /// </value>
        public override byte IndexId => 8;
        /// <summary>
        /// Gets the file offset.
        /// </summary>
        /// <value>
        /// The file offset.
        /// </value>
        public override int ArchiveEntryOffset => 0;
        /// <summary>
        /// Gets the size of the archive entry.
        /// </summary>
        /// <value>
        /// The size of the archive entry.
        /// </value>
        public override int ArchiveEntrySize => 1;

        /// <summary>
        /// Gets the size of the archive.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <returns></returns>
        public override int GetArchiveSize(ICacheAPI cache) => cache.GetFileCount(IndexId);
    }
}
