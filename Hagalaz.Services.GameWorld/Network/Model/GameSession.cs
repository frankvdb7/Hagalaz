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
        public long SessionGeneration { get; init; }
        public string SessionClaimId { get; }

        public GameSession(uint masterId, string connectionId, long sessionGeneration, string sessionClaimId, IRaidoClientProxy clientProxy)
        {
            MasterId = masterId;
            ConnectionId = connectionId;
            SessionGeneration = sessionGeneration;
            SessionClaimId = sessionClaimId;
            _clientProxy = clientProxy;
        }

        public void SendMessage(RaidoMessage message) => _clientProxy.SendAsync(message).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public sealed class WorldGameSession : GameSession, IGameWorldSession
    {
        public WorldGameSession(
            uint masterId,
            string connectionId,
            long sessionGeneration,
            IRaidoClientProxy clientProxy,
            string sessionClaimId)
            : base(masterId, connectionId, sessionGeneration, sessionClaimId, clientProxy)
        {
        }
    }
}
