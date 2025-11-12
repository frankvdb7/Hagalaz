using System.Threading.Tasks;
﻿using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Services.Model;

namespace Hagalaz.Services.GameWorld.Logic.Hydrators
{
    public class ProfileHydrator : ICharacterHydrator
    {
        public Task HydrateAsync(ICharacter character, CharacterModel model)
        {
            if (character is IHydratable<HydratedProfileDto> hydratable)
            {
                hydratable.Hydrate(model.Profile);
            }

            return Task.CompletedTask;
        }
    }
}
