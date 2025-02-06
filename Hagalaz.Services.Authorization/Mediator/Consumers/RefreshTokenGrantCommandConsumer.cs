using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Authorization.Mediator.Commands;
using Hagalaz.Services.Authorization.Model;

namespace Hagalaz.Services.Authorization.Mediator.Consumers
{
    public class RefreshTokenGrantCommandConsumer : IConsumer<RefreshTokenGrantCommand>
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly SignInManager<Character> _signInManager;

        public RefreshTokenGrantCommandConsumer(
            IHttpContextAccessor contextAccessor,
            SignInManager<Character> signInManager)
        {
            _contextAccessor = contextAccessor;
            _signInManager = signInManager;
        }

        public async Task Consume(ConsumeContext<RefreshTokenGrantCommand> context)
        {
            var message = context.Message;
            var httpContext = _contextAccessor.HttpContext ?? throw new InvalidOperationException("The OpenID Connect context cannot be retrieved.");
            var request = httpContext.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            // Retrieve the claims principal stored in the refresh token.
            var info = await httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            // Retrieve the user profile corresponding to the refresh token.
            // invalid refresh tokens / password / role changes require authentication again
            var user = await _signInManager.ValidateSecurityStampAsync(info.Principal);
            if (user == null)
            {
                var properties = new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The refresh token is no longer valid."
                });
                await context.RespondAsync(new RefreshTokenGrantResult(properties));
                return;
            }

            // Ensure the user is still allowed to sign in.
            if (!await _signInManager.CanSignInAsync(user))
            {
                var properties = new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is no longer allowed to sign in."
                });
                await context.RespondAsync(new RefreshTokenGrantResult(properties));
                return;
            }

            // Create a new ClaimsPrincipal containing the claims that
            // will be used to create an id_token, a token or a code.
            var principal = await _signInManager.CreateUserPrincipalAsync(user);
            await context.RespondAsync(new RefreshTokenGrantResult(principal));
        }
    }
}