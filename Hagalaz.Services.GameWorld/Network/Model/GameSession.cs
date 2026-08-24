using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Services.GameWorld.Model;
using Raido.Common.Protocol;
using Raido.Server;

namespace Hagalaz.Services.GameWorld.Network.Model
{
    public class GameSession : IGameSession
    {
        private readonly System.Threading.Lock _clientProxyLock = new();
        private IRaidoClientProxy _clientProxy;

        public uint MasterId { get; init; }
        public string ConnectionId { get; init; }

        public GameSession(uint masterId, string connectionId, IRaidoClientProxy clientProxy)
        {
            MasterId = masterId;
            ConnectionId = connectionId;
            _clientProxy = clientProxy;
        }

        public void SendMessage(RaidoMessage message)
        {
            lock (_clientProxyLock)
            {
                _clientProxy.SendAsync(message).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        internal void ExecuteWithClientProxy(IRaidoClientProxy reconnectProxy, System.Action action)
        {
            System.ArgumentNullException.ThrowIfNull(reconnectProxy);
            System.ArgumentNullException.ThrowIfNull(action);
            lock (_clientProxyLock)
            {
                var previousProxy = _clientProxy;
                _clientProxy = reconnectProxy;
                try { action(); }
                finally { _clientProxy = previousProxy; }
            }
        }
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
