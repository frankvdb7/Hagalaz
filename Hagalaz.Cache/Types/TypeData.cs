using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Types.Data;

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
        /// Gets the exclusive upper bound of the type IDs represented by this cache index.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <returns></returns>
        public virtual int GetArchiveSize(ICacheAPI cache)
        {
            var archiveSize = 0;
            foreach (var (archiveId, archive) in cache.ReadReferenceTable(IndexId).Entries)
            {
                var archiveEntrySize = GetArchiveEntryIdUpperBound(archive);
                if (archiveEntrySize == 0)
                {
                    continue;
                }

                var archiveUpperBound = checked((archiveId * ArchiveEntrySize) + archiveEntrySize);
                if (archiveUpperBound > archiveSize)
                {
                    archiveSize = archiveUpperBound;
                }
            }

            return archiveSize;
        }

        protected static int GetArchiveEntryIdUpperBound(IReferenceTableEntry? archive)
        {
            var upperBound = 0;
            if (archive is null)
            {
                return upperBound;
            }

            foreach (var (entryId, _) in archive.Entries)
            {
                var entryUpperBound = checked(entryId + 1);
                if (entryUpperBound > upperBound)
                {
                    upperBound = entryUpperBound;
                }
            }

            return upperBound;
        }

        /// <summary>
        /// Gets the archive identifier.
        /// </summary>
        /// <param name="typeId">The type identifier.</param>
        /// <returns></returns>
        public virtual int GetArchiveId(int typeId) => (int)((uint)typeId >> ArchiveEntryOffset);

        /// <summary>
        /// Gets the archive entry identifier.
        /// </summary>
        /// <param name="typeId">The type identifier.</param>
        /// <returns></returns>
        public virtual int GetArchiveEntryId(int typeId) => typeId & (1 << ArchiveEntryOffset) - 1;
    }
}
