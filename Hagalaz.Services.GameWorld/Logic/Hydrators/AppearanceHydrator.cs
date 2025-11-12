using System.Threading.Tasks;
﻿using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Services.Model;
using System.Threading.Tasks;

namespace Hagalaz.Services.GameWorld.Logic.Hydrators
{
    public class AppearanceHydrator : ICharacterHydrator
    {
        public Task HydrateAsync(ICharacter character, CharacterModel model)
        {
            if (character is IHydratable<HydratedAppearanceDto> hdt)
            {
                hdt.Hydrate(model.Appearance);
            }

            return Task.CompletedTask;
        }
    }
}
