using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Raido.Common.Protocol;

namespace Raido.Server
{
    public interface IRaidoLifetimeManager
    {
        Task OnConnectedAsync(RaidoConnectionContext connection);
        Task OnDisconnectedAsync(RaidoConnectionContext connection);
        Task SendAllAsync(RaidoMessage message, CancellationToken cancellationToken);
        Task SendAllExceptAsync(RaidoMessage message, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken);
        Task SendConnectionAsync(RaidoMessage message, string connectionId, CancellationToken cancellationToken);

        Task SendConnectionsAsync(RaidoMessage message, IReadOnlyList<string> connectionIds, CancellationToken cancellationToken);
    }
}