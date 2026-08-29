using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Http.Features;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Model
{
    public interface IGameConnection
    {
        string ConnectionId { get; }
        IFeatureCollection Features { get; }

        Task SendMessage(RaidoMessage message, CancellationToken cancellationToken = default);
    }
}