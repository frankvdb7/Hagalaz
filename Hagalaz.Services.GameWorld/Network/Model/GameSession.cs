using Hagalaz.Game.Abstractions.Model;
using Raido.Common.Protocol;
using Raido.Server;

namespace Hagalaz.Services.GameWorld.Network.Model
{
    public class GameSession : IGameSession
    {
        private readonly IRaidoClientProxy _clientProxy;

        public uint MasterId { get; init; }
        public string ConnectionId { get; init; }

        public GameSession(uint masterId, string connectionId, IRaidoClientProxy clientProxy)
        {
            MasterId = masterId;
            ConnectionId = connectionId;
            _clientProxy = clientProxy;
        }

        public void SendMessage(RaidoMessage message) => _clientProxy.SendAsync(message).ConfigureAwait(false).GetAwaiter().GetResult();
    }
}