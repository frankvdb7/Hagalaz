using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Features
{
    public interface ICharacterFeature
    {
        ICharacter Character { get; }
    }
}
