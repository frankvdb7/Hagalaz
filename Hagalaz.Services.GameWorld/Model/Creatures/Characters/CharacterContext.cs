using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    public class CharacterContext : ICharacterContext
    {
        public ICharacter Character { get; private set; }

        public CharacterContext(ICharacter character) => Character = character;
    }
}
