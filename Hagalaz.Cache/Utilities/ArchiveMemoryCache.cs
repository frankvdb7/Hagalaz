using System.IO;
using Hagalaz.Cache.Abstractions.Model;

namespace Hagalaz.Cache.Utilities
{
    /// <summary>
    /// 
    /// </summary>
    public class ArchiveMemoryCache
    {
        /// <summary>
        /// The archives
        /// </summary>
        private readonly IArchive?[] _archives;
        /// <summary>
        /// The cache
        /// </summary>
        private readonly CacheApi _cache;
        /// <summary>
        /// The table
        /// </summary>
        private readonly IReferenceTable _table;
        /// <summary>
        /// The index identifier
        /// </summary>
        private readonly byte _indexId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArchiveMemoryCache" /> class.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="indexId">The index identifier.</param>
        public ArchiveMemoryCache(CacheApi cache, byte indexId)
        {
            _cache = cache;
            _indexId = indexId;
            _table = cache.ReadReferenceTable(indexId);
            _archives = new IArchive[_cache.GetFileCount(indexId)];
        }

        /// <summary>
        /// Gets the archive entry.
        /// </summary>
        /// <param name="fileId">The file identifier.</param>
        /// <param name="subFileId">The sub file identifier.</param>
        /// <returns></returns>
        public MemoryStream? GetArchiveEntry(int fileId, int subFileId)
        {
            if (_archives[fileId] == null)
            {
                _archives[fileId] = _cache.ReadArchive(_indexId, fileId);
            }
            var childEntry = _table.GetEntry(fileId, subFileId);
            return childEntry == null ? null : _archives[fileId]?.GetEntry(childEntry.Index);
        }
    }
}
