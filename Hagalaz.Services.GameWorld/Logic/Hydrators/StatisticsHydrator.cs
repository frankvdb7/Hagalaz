using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Services.Model;

namespace Hagalaz.Services.GameWorld.Logic.Hydrators
{
    public class StatisticsHydrator : ICharacterHydrator
    {
        public void Hydrate(ICharacter character, CharacterModel model)
        {
            if (character is IHydratable<HydratedStatisticsDto> hdt)
            {
                hdt.Hydrate(model.Statistics);
            }
        }
    }
}
