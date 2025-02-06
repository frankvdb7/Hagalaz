﻿using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Services.Model;

namespace Hagalaz.Services.GameWorld.Logic.Dehydrators
{
    public interface ICharacterDehydrator
    {
        public CharacterModel Dehydrate(ICharacter character, CharacterModel model);
    }
}
