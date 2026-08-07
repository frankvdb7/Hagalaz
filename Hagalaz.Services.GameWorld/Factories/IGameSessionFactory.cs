using Hagalaz.Game.Abstractions.Model;

using Hagalaz.Services.GameWorld.Model;

namespace Hagalaz.Services.GameWorld.Factories
{
    public interface IGameSessionFactory
    {
        IGameSession Create(uint masterId, string connectionId);
        IGameWorldSession CreateWorld(uint masterId, string connectionId);
    }
}
