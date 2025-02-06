using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Factories
{
    public interface ICharacterFactory
    {
        ICharacter Create(IGameSession gameSession, IGameClient gameClient);
    }
}