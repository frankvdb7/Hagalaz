using System.Threading.Tasks;
﻿using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Services.Model;

namespace Hagalaz.Services.GameWorld.Services
{
    public interface ICharacterHydrationService
    {
        Task<bool> HydrateAsync(ICharacter character, CharacterModel model);
    }
}
