using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Services.GameWorld.Factories
{
    public interface IGameSessionFactory
    {
        IGameSession Create(uint masterId, string connectionId);
    }
}
