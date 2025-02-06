using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Raido.Common.Messages;
using Raido.Common.Protocol;

namespace Raido.Server.Internal
{
    internal class DefaultRaidoDispatcher : IRaidoDispatcher
    {
        private readonly IEnumerable<IRaidoHubDispatcher> _hubDispatchers;

        public DefaultRaidoDispatcher(IEnumerable<IRaidoHubDispatcher> hubDispatchers) => _hubDispatchers = hubDispatchers;
        
        public Task OnConnectedAsync(RaidoConnectionContext connection) =>
            Task.WhenAll(_hubDispatchers.Select(d => d.OnConnectedAsync(connection)));

        public Task OnDisconnectedAsync(RaidoConnectionContext connection, Exception? exception) =>
            Task.WhenAll(_hubDispatchers.Select(d => d.OnDisconnectedAsync(connection, exception)));

        public Task DispatchMessageAsync(RaidoConnectionContext connection, RaidoMessage message)
        {
            if (message is not PingMessage)
            {
                return Task.WhenAll(_hubDispatchers.Select(d => d.DispatchMessageAsync(connection, message)));
            }

            // message is a ping message
            connection.StartClientTimeout();
            return Task.CompletedTask;

        }
    }
}