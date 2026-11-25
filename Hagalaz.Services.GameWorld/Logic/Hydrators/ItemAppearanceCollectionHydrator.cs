using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Services.Model;

namespace Hagalaz.Services.GameWorld.Logic.Hydrators
{
    public class ItemAppearanceCollectionHydrator : ICharacterHydrator
    {
        public void Hydrate(ICharacter character, CharacterModel model)
        {
            if (character.Appearance is IHydratable<HydratedItemAppearanceCollectionDto> hydratable)
            {
                hydratable.Hydrate(model.ItemAppearanceCollection);
            }
        }
    }
}
