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

        public Task<(IGameSession Session, bool Created)> AddSession(uint masterId, string connectionId)
        {
            if (_sessions.TryGetValue(connectionId, out var existingSession))
            {
                return Task.FromResult((existingSession, Created: false));
            }

            var createdSession = _gameSessionFactory.Create(masterId, connectionId);
            var session = _sessions.GetOrAdd(connectionId, _ => createdSession);
            return Task.FromResult((session, Created: ReferenceEquals(session, createdSession)));
        }

        public Task<bool> RemoveSession(string connectionId) => Task.FromResult(_sessions.TryRemove(connectionId));

        public Task<IGameSession?> FindByMasterId(uint masterId) => Task.FromResult(_sessions.FirstOrDefault(session => session.MasterId == masterId));
    }
}
