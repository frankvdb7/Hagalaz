using System.Threading.Tasks;
﻿using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Services.Model;

namespace Hagalaz.Services.GameWorld.Logic.Hydrators
{
    public class FarmingHydrator : ICharacterHydrator
    {
        public Task HydrateAsync(ICharacter character, CharacterModel model)
        {
            if (character is IHydratable<HydratedFarmingDto> hydratable)
            {
                hydratable.Hydrate(model.Farming);
            }

            return Task.CompletedTask;
        }
    }
}
