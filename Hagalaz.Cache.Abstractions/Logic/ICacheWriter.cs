using Hagalaz.Cache.Abstractions.Model;

namespace Hagalaz.Cache.Abstractions.Logic
{
    /// <summary>
    /// Defines the contract for a cache writer, which handles writing data containers to the cache.
    /// </summary>
    public interface ICacheWriter
    {
        /// <summary>
        /// Writes a data container to an existing archive in the cache. Its reference-table entry must already exist because this operation does not supply child-member mappings.
        /// </summary>
        /// <param name="indexId">The identifier of the index (cache) to write to.</param>
        /// <param name="fileId">The identifier of the file to write.</param>
        /// <param name="container">The container holding the data to be written.</param>
        /// <exception cref="System.IO.IOException">Thrown if the archive has no existing reference-table entry.</exception>
        void Write(int indexId, int fileId, IContainer container);
    }
}
