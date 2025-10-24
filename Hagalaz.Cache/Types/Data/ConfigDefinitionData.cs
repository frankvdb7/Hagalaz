namespace Hagalaz.Cache.Types.Data
{
    /// <summary>
    /// Provides the cache location for config definitions.
    /// </summary>
    public class ConfigDefinitionData : TypeData
    {
        /// <inheritdoc />
        public override byte IndexId => 2;

        /// <inheritdoc />
        public override int ArchiveEntryOffset => 0;

        /// <inheritdoc />
        public override int ArchiveEntrySize => 1;

        /// <summary>
        /// Gets the total number of definitions based on the number of files in the archive.
        /// </summary>
        /// <param name="cache">The cache API.</param>
        /// <returns>The total number of definitions.</returns>
        public override int GetArchiveSize(ICacheAPI cache) => cache.GetFileCount(IndexId, 16);

        /// <summary>
        /// Gets the archive identifier for the specified type identifier.
        /// All config definitions are stored in archive 16.
        /// </summary>
        /// <param name="typeId">The type identifier (ignored).</param>
        /// <returns>The archive identifier, which is always 16.</returns>
        public override int GetArchiveId(int typeId) => 16;

        /// <summary>
        /// Gets the archive entry identifier for the specified type identifier.
        /// The type ID corresponds directly to the file ID within the archive.
        /// </summary>
        /// <param name="typeId">The type identifier.</param>
        /// <returns>The archive entry identifier.</returns>
        public override int GetArchiveEntryId(int typeId) => typeId;
    }
}
