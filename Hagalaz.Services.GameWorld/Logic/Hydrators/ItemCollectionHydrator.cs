using System.Threading.Tasks;
﻿using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Services.Model;

namespace Hagalaz.Services.GameWorld.Logic.Hydrators
{
    public class ItemCollectionHydrator : ICharacterHydrator
    {
        public Task HydrateAsync(ICharacter character, CharacterModel model)
        {
            if (character is IHydratable<HydratedItemCollectionDto> hdt)
            {
                hdt.Hydrate(model.ItemCollection);
            }

            return Task.CompletedTask;
        }
    }
}
