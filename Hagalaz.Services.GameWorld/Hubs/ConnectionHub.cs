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
            var signOutSucceeded = false;
            try
            {
                await _authenticationService.SignOutAsync();
                signOutSucceeded = true;
            }
            finally
            {
                var character = Context.GetCharacter();
                // A failed sign out leaves the live character registered so a later persistence
                // attempt can retry it. Never destroy that instance while it remains in the store.
                if (signOutSucceeded && character is { IsDestroyed: false })
                {
                    character.Destroy();
                }
            }
        }
    }
}
