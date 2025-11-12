using System.Threading.Tasks;
﻿using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Services.Model;

namespace Hagalaz.Services.GameWorld.Logic.Dehydrators
{
    public interface ICharacterDehydrator
    {
        Task<CharacterModel> DehydrateAsync(ICharacter character, CharacterModel model);
    }
}
