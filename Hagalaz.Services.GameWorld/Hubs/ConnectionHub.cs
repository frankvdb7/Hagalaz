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
        private readonly ICharacterPersistenceService? _characterPersistenceService;

        public ConnectionHub(IAuthenticationService authenticationService)
            : this(authenticationService, null)
        {
        }

        public ConnectionHub(
            IAuthenticationService authenticationService,
            ICharacterPersistenceService? characterPersistenceService)
        {
            _authenticationService = authenticationService;
            _characterPersistenceService = characterPersistenceService;
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
                // A successful outbox handoff may still await consumer acknowledgement. Keep
                // detached pending characters alive for the worker to redrive and clean up.
                if (signOutSucceeded && character is { IsDestroyed: false } &&
                    (_characterPersistenceService is null || !_characterPersistenceService.IsPendingLogout(character)))
                {
                    character.Destroy();
                }
            }
        }
    }
}
