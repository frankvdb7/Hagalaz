using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Http.Features;
using Raido.Common.Protocol;
using Raido.Server;

namespace Hagalaz.Services.GameWorld.Network.Model
{
    public interface IGameConnection
    {
        string ConnectionId { get; }
        IFeatureCollection Features { get; }

        Task<bool> TryReconnectAsync(
            RaidoCallerContext replacement,
            IRaidoProtocol replacementProtocol,
            Func<ValueTask<bool>> completeHandshake);

        Task SendMessage(RaidoMessage message, CancellationToken cancellationToken = default);
    }
}
