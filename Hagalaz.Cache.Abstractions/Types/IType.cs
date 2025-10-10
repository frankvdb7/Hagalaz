namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// Defines the base contract for all data types that can be loaded from the game cache.
    /// This interface ensures that every cacheable type has a unique identifier.
    /// </summary>
    public interface IType
    {
        /// <summary>
        /// Gets the unique identifier for this cache type.
        /// </summary>
        int Id { get; }
    }
}