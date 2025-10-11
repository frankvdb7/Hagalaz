using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Factories
{
    /// <summary>
    /// Defines a contract for a factory that creates instances of item containers.
    /// </summary>
    public interface IItemContainerFactory
    {
        /// <summary>
        /// Creates a new familiar inventory container for a specified character.
        /// </summary>
        /// <param name="character">The character who owns the familiar.</param>
        /// <param name="storageType">The storage behavior type for the container.</param>
        /// <param name="capacity">The maximum number of slots the container can hold.</param>
        /// <returns>A new <see cref="IFamiliarInventoryContainer"/> instance.</returns>
        public IFamiliarInventoryContainer Create(ICharacter character, StorageType storageType, int capacity);
    }
}
