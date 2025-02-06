using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Services.Model;

namespace Hagalaz.Services.GameWorld.Services
{
    public interface ICharacterDehydrationService
    {
        public CharacterModel Dehydrate(ICharacter character);
    }
}
