using System.Threading.Tasks;
using System;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.Extensions.Logging;
using Raido.Server;

namespace Hagalaz.Services.GameWorld.Hubs
{
    public class ConnectionHub : RaidoHub
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<ConnectionHub> _logger;

        public ConnectionHub(
            IAuthenticationService authenticationService,
            ILogger<ConnectionHub> logger)
        {
            _authenticationService = authenticationService;
            _logger = logger;
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation(
                exception,
                "Game client connection '{connectionId}' disconnected; signing out its session.",
                Context.ConnectionId);

            await _authenticationService.SignOutAsync();
        }
    }
}
