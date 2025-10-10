namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// Defines a contract for a provider that retrieves and manages instances of a specific
    /// cacheable type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The cacheable type that this provider handles.</typeparam>
    public interface ITypeProvider<out T>
    {
        /// <summary>
        /// Gets the total number of entries available for this type in its corresponding cache archive.
        /// </summary>
        int ArchiveSize { get; }

        /// <summary>
        /// Retrieves all instances of the type <typeparamref name="T"/> from the cache.
        /// </summary>
        /// <returns>An array containing all instances of <typeparamref name="T"/>.</returns>
        T[] GetAll();

        /// <summary>
        /// Retrieves a range of instances of the type <typeparamref name="T"/> from the cache, identified by their IDs.
        /// </summary>
        /// <param name="startTypeId">The starting type ID of the range.</param>
        /// <param name="endTypeId">The ending type ID of the range.</param>
        /// <returns>An array containing the instances of <typeparamref name="T"/> within the specified ID range.</returns>
        T[] GetRange(int startTypeId, int endTypeId);

        /// <summary>
        /// Retrieves a single instance of the type <typeparamref name="T"/> from the cache by its unique ID.
        /// </summary>
        /// <param name="typeId">The unique identifier of the type to retrieve.</param>
        /// <returns>The instance of <typeparamref name="T"/> corresponding to the specified ID.</returns>
        T Get(int typeId);
    }
}
