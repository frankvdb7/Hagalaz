using Hagalaz.Cache.Types.Data;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="ITypeData" />
    public abstract class TypeData : ITypeData
    {
        /// <summary>
        /// Gets the index identifier.
        /// </summary>
        /// <value>
        /// The index identifier.
        /// </value>
        public abstract byte IndexId { get; }
        /// <summary>
        /// Gets the file offset.
        /// </summary>
        /// <value>
        /// The file offset.
        /// </value>
        public abstract int ArchiveEntryOffset { get; }
        /// <summary>
        /// Gets the size of the archive entry.
        /// </summary>
        /// <value>
        /// The size of the archive entry.
        /// </value>
        public abstract int ArchiveEntrySize { get; }

        /// <summary>
        /// Gets the size of the archive.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <returns></returns>
        public virtual int GetArchiveSize(ICacheAPI cache)
        {
            var lastFileId = cache.GetFileCount(IndexId) - 1;
            return (lastFileId * ArchiveEntrySize) + cache.GetFileCount(IndexId, lastFileId - 1);
        }
    }
}
