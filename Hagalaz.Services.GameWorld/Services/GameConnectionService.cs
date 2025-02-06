using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Services.GameWorld.Network.Model;
using Raido.Server;

namespace Hagalaz.Services.GameWorld.Services
{
    public class GameConnectionService : IGameConnectionService
    {
        private readonly RaidoConnectionStore _connections;
        private readonly IGameSessionService _gameSessionService;

        public GameConnectionService(RaidoConnectionStore connections, IGameSessionService gameSessionService)
        {
            _connections = connections;
            _gameSessionService = gameSessionService;
        }

        public async IAsyncEnumerable<IGameConnection> FindAll()
        {
            await Task.CompletedTask;
            foreach (var connection in _connections)
            {
                yield return new GameConnection(connection);
            }
        }

        public async Task<IGameConnection?> FindById(string connectionId)
        {
            await Task.CompletedTask;
            var connection = _connections[connectionId];
            return connection == null ? null : new GameConnection(connection);
        }

        public async Task<IGameConnection?> FindByMasterId(uint masterId)
        {
            var session = await _gameSessionService.FindByMasterId(masterId);
            if (session == null)
            {
                return null;
            }
            return await FindById(session.ConnectionId);
        }
    }
}
