namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// Defines a provider for a specific type from the cache.
    /// </summary>
    /// <typeparam name="T">The type to provide.</typeparam>
    public interface ITypeProvider<out T>
    {
        /// <summary>
        /// Gets the total number of entries in the archive for this type.
        /// </summary>
        int ArchiveSize { get; }

        /// <summary>
        /// Gets all instances of the type from the cache.
        /// </summary>
        /// <returns>An array containing all instances of the type.</returns>
        T[] GetAll();

        /// <summary>
        /// Gets a range of instances of the type from the cache.
        /// </summary>
        /// <param name="startTypeId">The starting type ID.</param>
        /// <param name="endTypeId">The ending type ID.</param>
        /// <returns>An array containing the instances within the specified range.</returns>
        T[] GetRange(int startTypeId, int endTypeId);

        /// <summary>
        /// Gets a single instance of the type from the cache.
        /// </summary>
        /// <param name="typeId">The ID of the type to get.</param>
        /// <returns>The instance of the type.</returns>
        T Get(int typeId);
    }
}
