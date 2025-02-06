using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Features
{
    public class CharacterFeature : ICharacterFeature
    {
        public required ICharacter Character { get; init; }
    }
}
