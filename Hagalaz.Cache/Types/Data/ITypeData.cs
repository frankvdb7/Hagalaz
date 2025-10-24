namespace Hagalaz.Cache.Types.Data
{
    /// <summary>
    /// 
    /// </summary>
    public interface ITypeData
    {
        /// <summary>
        /// Gets the index identifier.
        /// </summary>
        /// <value>
        /// The index identifier.
        /// </value>
        byte IndexId { get; }
        /// <summary>
        /// Gets the file offset.
        /// </summary>
        /// <value>
        /// The file offset.
        /// </value>
        int ArchiveEntryOffset { get; }
        /// <summary>
        /// Gets the size of the archive entry.
        /// </summary>
        /// <value>
        /// The size of the archive entry.
        /// </value>
        int ArchiveEntrySize { get; }

        /// <summary>
        /// Gets the size of the archive.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <returns></returns>
        int GetArchiveSize(ICacheAPI cache);

        /// <summary>
        /// Gets the archive identifier.
        /// </summary>
        /// <param name="typeId">The type identifier.</param>
        /// <returns></returns>
        int GetArchiveId(int typeId);

        /// <summary>
        /// Gets the archive entry identifier.
        /// </summary>
        /// <param name="typeId">The type identifier.</param>
        /// <returns></returns>
        int GetArchiveEntryId(int typeId);
    }
}
