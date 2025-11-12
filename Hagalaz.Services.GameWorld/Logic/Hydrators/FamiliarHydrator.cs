using System.Threading.Tasks;
﻿using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Services.Model;

namespace Hagalaz.Services.GameWorld.Logic.Hydrators
{
    public class FamiliarHydrator : ICharacterHydrator
    {
        public Task HydrateAsync(ICharacter character, CharacterModel model)
        {
            if (model.Familiar != null && character is IHydratable<HydratedFamiliarDto> hdt)
            {
                hdt.Hydrate(model.Familiar);
            }

            return Task.CompletedTask;
        }
    }
}
