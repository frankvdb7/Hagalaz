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
        /// Decodes all instances of the type from the cache.
        /// </summary>
        /// <returns>An array containing all decoded instances of the type.</returns>
        T[] DecodeAll();

        /// <summary>
        /// Decodes a range of instances of the type from the cache.
        /// </summary>
        /// <param name="startTypeId">The starting type ID.</param>
        /// <param name="endTypeId">The ending type ID.</param>
        /// <returns>An array containing the decoded instances within the specified range.</returns>
        T[] DecodeRange(int startTypeId, int endTypeId);

        /// <summary>
        /// Decodes a single instance of the type from the cache.
        /// </summary>
        /// <param name="typeId">The ID of the type to decode.</param>
        /// <returns>The decoded instance of the type.</returns>
        T Decode(int typeId);
    }
}
