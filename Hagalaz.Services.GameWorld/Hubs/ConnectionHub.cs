using System.Threading.Tasks;
using System;
using Hagalaz.Services.GameWorld.Services;
using Raido.Server;

namespace Hagalaz.Services.GameWorld.Hubs
{
    public class ConnectionHub : RaidoHub
    {
        private readonly IAuthenticationService _authenticationService;
        public ConnectionHub(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public override Task OnDisconnectedAsync(Exception? exception) => _authenticationService.SignOutAsync();
    }
}
