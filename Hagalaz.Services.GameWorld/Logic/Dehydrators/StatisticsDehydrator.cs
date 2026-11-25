using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Services.Model;

namespace Hagalaz.Services.GameWorld.Logic.Dehydrators
{
    public class StatisticsDehydrator : ICharacterDehydrator
    {
        public CharacterModel Dehydrate(ICharacter character, CharacterModel model)
        {
            if (character is IDehydratable<HydratedStatisticsDto> dehydratable)
            {
                return model with { Statistics = dehydratable.Dehydrate() };
            }
            return model;
        }
    }
}
