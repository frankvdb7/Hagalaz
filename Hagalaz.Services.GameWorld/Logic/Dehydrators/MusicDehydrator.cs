using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Services.Model;

namespace Hagalaz.Services.GameWorld.Logic.Dehydrators
{
    public class MusicDehydrator : ICharacterDehydrator
    {
        public CharacterModel Dehydrate(ICharacter character, CharacterModel model)
        {
            if (character is IDehydratable<HydratedMusicDto> dehydratable)
            {
                return model with { Music = dehydratable.Dehydrate() };
            }
            return model;
        }
    }
}
