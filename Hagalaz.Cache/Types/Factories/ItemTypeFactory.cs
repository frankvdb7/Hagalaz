using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types.Factories
{
    /// <summary>
    /// A factory for creating <see cref="IItemType"/> instances.
    /// </summary>
    public class ItemTypeFactory : ITypeFactory<IItemType>
    {
        /// <summary>
        /// Creates a new <see cref="IItemType"/> with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the item type.</param>
        /// <returns>A new <see cref="IItemType"/> instance.</returns>
        public IItemType CreateType(int id) => new ItemType(id);
    }
}