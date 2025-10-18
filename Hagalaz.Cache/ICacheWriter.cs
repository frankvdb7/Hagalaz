namespace Hagalaz.Cache
{
    /// <summary>
    /// Defines the contract for a cache writer, which handles writing data containers to the cache.
    /// </summary>
    public interface ICacheWriter
    {
        /// <summary>
        /// Writes a data container to a specific file in the cache.
        /// </summary>
        /// <param name="indexId">The identifier of the index (cache) to write to.</param>
        /// <param name="fileId">The identifier of the file to write.</param>
        /// <param name="container">The container holding the data to be written.</param>
        void Write(int indexId, int fileId, IContainer container);
    }
}
