using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Http.Features;
using Raido.Common.Protocol;
using Raido.Server;

namespace Hagalaz.Services.GameWorld.Network.Model
{
    public class GameConnection : IGameConnection
    {
        private readonly RaidoConnectionContext _context;

        public GameConnection(RaidoConnectionContext context) => _context = context;

        public string ConnectionId => _context.ConnectionId;
        public IFeatureCollection Features => _context.Features;

        public async Task SendMessage(RaidoMessage message, CancellationToken cancellationToken = default) => await _context.WriteAsync(message, cancellationToken);
    }
}
