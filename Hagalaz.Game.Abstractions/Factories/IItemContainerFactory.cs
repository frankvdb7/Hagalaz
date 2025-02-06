using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Factories
{
    public interface IItemContainerFactory
    {
        public IFamiliarInventoryContainer Create(ICharacter character, StorageType storageType, int capacity);
    }
}
