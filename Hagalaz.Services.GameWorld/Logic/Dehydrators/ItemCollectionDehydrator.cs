using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Hagalaz.Services.GameWorld.Services.Model;

namespace Hagalaz.Services.GameWorld.Logic.Dehydrators
{
    public class ItemCollectionDehydrator : ICharacterDehydrator
    {
        public CharacterModel Dehydrate(ICharacter character, CharacterModel model)
        {
            if (character is IDehydratable<HydratedItemCollectionDto> dehydratable)
            {
                return model with { ItemCollection = dehydratable.Dehydrate() };
            }
            return model;
        }
    }
}
