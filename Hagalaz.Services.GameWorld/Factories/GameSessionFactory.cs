using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Services.GameWorld.Model;
using Hagalaz.Services.GameWorld.Network.Model;
using Raido.Server;
using System;

namespace Hagalaz.Services.GameWorld.Factories
{
    public class GameSessionFactory : IGameSessionFactory
    {
        private readonly IRaidoHubLifetimeManager _lifetimeManager;

        public GameSessionFactory(IRaidoHubLifetimeManager lifetimeManager) => _lifetimeManager = lifetimeManager;

        public IGameSession Create(uint masterId, string connectionId, long sessionGeneration) =>
            new GameSession(masterId, connectionId, sessionGeneration, Guid.NewGuid().ToString("N"),
                new GameClientProxy(_lifetimeManager, connectionId));

        public IGameWorldSession CreateWorld(uint masterId, string connectionId, long sessionGeneration) =>
            new WorldGameSession(masterId, connectionId, sessionGeneration, new GameClientProxy(_lifetimeManager, connectionId),
                Guid.NewGuid().ToString("N"));
    }
}
