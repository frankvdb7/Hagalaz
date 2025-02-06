using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Services.GameWorld.Factories;
using Hagalaz.Services.GameWorld.Store;

namespace Hagalaz.Services.GameWorld.Services
{
    public class GameSessionService : IGameSessionService
    {
        private readonly GameSessionStore _sessions;
        private readonly IGameSessionFactory _gameSessionFactory;

        public GameSessionService(GameSessionStore sessions, IGameSessionFactory gameSessionFactory)
        {
            _sessions = sessions;
            _gameSessionFactory = gameSessionFactory;
        }

        public async Task<IGameSession> AddSession(uint masterId, string connectionId)
        {
            await Task.CompletedTask;
            var session = _sessions.GetOrAdd(connectionId, _ => _gameSessionFactory.Create(masterId, connectionId));
            return session;
        }

        public Task<bool> RemoveSession(string connectionId) => Task.FromResult(_sessions.TryRemove(connectionId));

        public Task<IGameSession?> FindByMasterId(uint masterId) => Task.FromResult(_sessions.FirstOrDefault(session => session.MasterId == masterId));
    }
}