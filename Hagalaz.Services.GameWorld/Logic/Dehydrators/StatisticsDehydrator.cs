using System.Threading.Tasks;
﻿using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Services.Model;

namespace Hagalaz.Services.GameWorld.Logic.Dehydrators
{
    public class StatisticsDehydrator : ICharacterDehydrator
    {
        public Task<CharacterModel> DehydrateAsync(ICharacter character, CharacterModel model)
        {
            if (character is IDehydratable<HydratedStatisticsDto> dehydratable)
            {
                return Task.FromResult(model with { Statistics = dehydratable.Dehydrate() });
            }
            return Task.FromResult(model);
        }
    }
}
