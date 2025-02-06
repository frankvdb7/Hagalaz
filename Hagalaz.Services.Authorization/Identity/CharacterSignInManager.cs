using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.Authorization.Identity
{
    public class CharacterSignInManager : SignInManager<Character>
    {
        private readonly CharacterManager _characterManager;

        public CharacterSignInManager(
            CharacterManager characterManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<Character> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<CharacterSignInManager> logger,
            IAuthenticationSchemeProvider schemes,
            IUserConfirmation<Character> confirmation) : base(characterManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation) => _characterManager = characterManager;

        public override async Task<bool> CanSignInAsync(Character user)
        {
            if (await IsBannedAsync(user))
            {
                return false;
            }
            return await base.CanSignInAsync(user);
        }

        public Task<bool> IsBannedAsync(Character character) => _characterManager.IsBannedAsync(character);
    }
}