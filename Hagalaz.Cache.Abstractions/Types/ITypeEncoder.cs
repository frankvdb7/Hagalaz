namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// Defines a contract for an encoder that serializes a specific cacheable type into the cache.
    /// </summary>
    /// <typeparam name="T">The type of the object to be encoded, which must implement <see cref="IType"/>.</typeparam>
    public interface ITypeEncoder<in T> where T : IType
    {
        /// <summary>
        /// Encodes the given type instance into the cache, associating it with a specific type ID.
        /// </summary>
        /// <param name="typeId">The unique identifier for the type being encoded.</param>
        /// <param name="type">The instance of the type to encode.</param>
        void Encode(int typeId, T type);
    }
}
