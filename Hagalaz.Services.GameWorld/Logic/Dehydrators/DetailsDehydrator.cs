using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Hagalaz.Services.GameWorld.Services.Model;

namespace Hagalaz.Services.GameWorld.Logic.Dehydrators
{
    public class DetailsDehydrator : ICharacterDehydrator
    {
        public CharacterModel Dehydrate(ICharacter character, CharacterModel model)
        {
            if (character is IDehydratable<HydratedDetailsDto> dehydratable)
            {
                return model with
                {
                    Details = dehydratable.Dehydrate()
                };
            }
            return model;
        }
    }
}
