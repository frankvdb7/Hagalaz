using System.Threading.Tasks;
﻿using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Services.Model;

namespace Hagalaz.Services.GameWorld.Logic.Hydrators
{
    public interface ICharacterHydrator
    {
        Task HydrateAsync(ICharacter character, CharacterModel model);
    }
}
