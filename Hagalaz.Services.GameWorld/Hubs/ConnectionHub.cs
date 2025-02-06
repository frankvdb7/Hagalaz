using System.Threading.Tasks;
using System;
using Hagalaz.Services.GameWorld.Services;
using Raido.Server;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Hubs
{
    public class ConnectionHub : RaidoHub
    {
        private readonly IAuthenticationService _authenticationService;

        public ConnectionHub(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                await _authenticationService.SignOutAsync();
            }
            catch(Exception)
            {
                throw;
            }
            finally
            {
                var character = Context.GetCharacter();
                character?.Destroy();
            }
        }
    }
}
