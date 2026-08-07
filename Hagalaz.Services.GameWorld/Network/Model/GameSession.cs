using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Services.GameWorld.Model;
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

    public sealed class WorldGameSession : GameSession, IGameWorldSession
    {
        public string SessionClaimId { get; }

        public WorldGameSession(
            uint masterId,
            string connectionId,
            IRaidoClientProxy clientProxy,
            string sessionClaimId)
            : base(masterId, connectionId, clientProxy)
        {
            SessionClaimId = sessionClaimId;
        }
    }
}
