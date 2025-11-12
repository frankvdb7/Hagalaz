using System.Threading.Tasks;
﻿using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Services.Model;

namespace Hagalaz.Services.GameWorld.Services
{
    public interface ICharacterDehydrationService
    {
        Task<CharacterModel> DehydrateAsync(ICharacter character);
    }
}
