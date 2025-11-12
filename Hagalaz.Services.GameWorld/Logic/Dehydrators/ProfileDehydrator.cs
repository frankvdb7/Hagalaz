using System.Threading.Tasks;
﻿using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Services.Model;

namespace Hagalaz.Services.GameWorld.Logic.Dehydrators
{
    public class ProfileDehydrator : ICharacterDehydrator
    {
        public Task<CharacterModel> DehydrateAsync(ICharacter character, CharacterModel model)
        {
            if (character is IDehydratable<HydratedProfileDto> dehydratable)
            {
                return Task.FromResult(model with { Profile = dehydratable.Dehydrate() });
            }
            return Task.FromResult(model);
        }
    }
}
