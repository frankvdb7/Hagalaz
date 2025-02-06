using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Messages.Mediator;
using Hagalaz.Game.Messages.Protocol;
using Microsoft.AspNetCore.Authorization;
using Raido.Common.Protocol;
using Raido.Server;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Hubs
{
    [Authorize]
    public class LobbyHub : RaidoHub
    {
        private readonly IScopedGameMediator _mediator;

        public LobbyHub(IScopedGameMediator mediator) => _mediator = mediator;

        [RaidoMessageHandler(typeof(GetWorldInfoRequest))]
        public Task GetWorldInfo(GetWorldInfoRequest request) => _mediator.SendAsync(new SendWorldInfoCommand(Context.GetSession())
        {
            Checksum = request.Checksum
        });
    }
}
