using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Factories
{
    public class ItemContainerFactory : IItemContainerFactory
    {
        public IFamiliarInventoryContainer Create(ICharacter character, StorageType storageType, int capacity) => new FamiliarInventoryContainer(character, storageType, capacity);
    }
}
