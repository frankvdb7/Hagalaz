using System.Linq;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Logic.Characters.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Services.Model;

namespace Hagalaz.Services.GameWorld.Logic.Hydrators
{
    public class ItemCollectionHydrator(FamiliarRestorationState restorationState) : ICharacterHydrator
    {
        public void Hydrate(ICharacter character, CharacterModel model)
        {
            if (character is IHydratable<HydratedItemCollectionDto> hdt)
            {
                hdt.Hydrate(model.ItemCollection);
            }

            if (character.FamiliarScript is null && model.ItemCollection.FamiliarInventory.Count > 0)
            {
                restorationState.SetInventory(model.ItemCollection.FamiliarInventory
                    .Select(item => new HydratedItem(item.ItemId, item.Count, item.SlotId, item.ExtraData))
                    .ToList());
            }
        }
    }
}
