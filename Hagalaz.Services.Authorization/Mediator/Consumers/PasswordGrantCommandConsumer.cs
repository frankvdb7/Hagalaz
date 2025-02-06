using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Services.Authorization.Identity;
using Hagalaz.Services.Authorization.Mediator.Commands;
using Hagalaz.Services.Authorization.Model;
using MassTransit;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;

namespace Hagalaz.Services.Authorization.Mediator.Consumers
{
    public class PasswordGrantCommandConsumer : IConsumer<PasswordGrantCommand>
    {
        private readonly CharacterManager _characterManager;
        private readonly CharacterSignInManager _signInManager;
        private readonly OpenIddictScopeManager<OpenIddictEntityFrameworkCoreScope> _scopeManager;

        public PasswordGrantCommandConsumer(
            CharacterManager characterManager,
            CharacterSignInManager signInManager,
            OpenIddictScopeManager<OpenIddictEntityFrameworkCoreScope> scopeManager)
        {
            _characterManager = characterManager;
            _signInManager = signInManager;
            _scopeManager = scopeManager;
        }

        public async Task Consume(ConsumeContext<PasswordGrantCommand> context)
        {
            var message = context.Message;
            var character = await _characterManager.FindByLoginAsync(message.Login);
            if (character == null)
            {
                await context.RespondAsync(PasswordGrantResult.CredentialsInvalid);
                return;
            }
            var signInResult = await _signInManager.CheckPasswordSignInAsync(character, message.Password, lockoutOnFailure: true);
            if (signInResult.IsLockedOut)
            {
                await context.RespondAsync(PasswordGrantResult.LockedOut);
                return;
            }
            if (signInResult.IsNotAllowed)
            {
                await context.RespondAsync(PasswordGrantResult.NotAllowed);
                return;
            }
            if (!signInResult.Succeeded)
            {
                await context.RespondAsync(PasswordGrantResult.CredentialsInvalid);
                return;
            }

            // Create a new ClaimsPrincipal containing the claims that
            // will be used to create an id_token, a token or a code.
            var principal = await _signInManager.CreateUserPrincipalAsync(character);
            var scopes = message.Scopes;

            principal.SetScopes(scopes);
            principal.SetResources(await _scopeManager.ListResourcesAsync(scopes, context.CancellationToken).ToListAsync(context.CancellationToken));

            await context.RespondAsync(new PasswordGrantResult(principal));
        }
    }
}