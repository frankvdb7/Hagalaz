using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Services.GameWorld.Model;
using Hagalaz.Services.GameWorld.Network.Model;
using Raido.Server;
using System;

namespace Hagalaz.Services.GameWorld.Factories
{
    public class GameSessionFactory : IGameSessionFactory
    {
        private readonly IRaidoLifetimeManager _lifetimeManager;

        public GameSessionFactory(IRaidoLifetimeManager lifetimeManager) => _lifetimeManager = lifetimeManager;

        public IGameSession Create(uint masterId, string connectionId) =>
            new GameSession(masterId, connectionId, new GameClientProxy(_lifetimeManager, connectionId));

        public IGameWorldSession CreateWorld(uint masterId, string connectionId) =>
            new WorldGameSession(masterId, connectionId, new GameClientProxy(_lifetimeManager, connectionId),
                Guid.NewGuid().ToString("N"));
    }
}
