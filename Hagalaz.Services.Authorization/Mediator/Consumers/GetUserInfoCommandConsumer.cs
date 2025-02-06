using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Authorization.Constants;
using Hagalaz.Services.Authorization.Identity;
using Hagalaz.Services.Authorization.Mediator.Commands;
using Hagalaz.Services.Authorization.Model;
using MassTransit;
using OpenIddict.Abstractions;

namespace Hagalaz.Services.Authorization.Mediator.Consumers
{
    public class GetUserInfoCommandConsumer : IConsumer<GetUserInfoCommand>
    {
        private readonly CharacterManager _characterManager;

        public GetUserInfoCommandConsumer(CharacterManager characterManager)
        {
            _characterManager = characterManager;
        }

        public async Task Consume(ConsumeContext<GetUserInfoCommand> context)
        {
            var message = context.Message;
            var principal = message.User;
            var user = await _characterManager.GetUserAsync(principal);
            if (user == null)
            {
                await context.RespondAsync(new GetUserInfoResult(null));
                return;
            }

#pragma warning disable CS8601 // Possible null reference assignment.
            var claims = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                // Note: the "sub" claim is a mandatory claim and must be included in the JSON response.
                [OpenIddictConstants.Claims.Subject] = await _characterManager.GetUserIdAsync(user)
            };

            if (principal.HasScope(OpenIddictConstants.Scopes.Profile))
            {
                claims[OpenIddictConstants.Claims.Username] = await _characterManager.GetUserNameAsync(user);
                claims[OpenIddictConstants.Claims.PreferredUsername] = await _characterManager.GetDisplayNameAsync(user);
                claims[Claims.PreviousPreferredUsername] = await _characterManager.GetPreviousDisplayNameAsync(user);
                var lastLogin = await _characterManager.GetLastLoginAsync(user);
                if (lastLogin != null)
                {
                    claims[Claims.LastLogin] = lastLogin;
                }
                var lastIp = await _characterManager.GetLastIpAsync(user);
                if (lastIp != null)
                {
                    claims[Claims.LastIp] = lastIp;
                }
            }

            if (principal.HasScope(OpenIddictConstants.Scopes.Email))
            {
                claims[OpenIddictConstants.Claims.Email] = await _characterManager.GetEmailAsync(user);
                claims[OpenIddictConstants.Claims.EmailVerified] = await _characterManager.IsEmailConfirmedAsync(user);
            }

            if (principal.HasScope(OpenIddictConstants.Scopes.Roles))
            {
                claims[OpenIddictConstants.Claims.Role] = await _characterManager.GetRolesAsync(user);
            }
#pragma warning restore CS8601 // Possible null reference assignment.

            // Note: the complete list of standard claims supported by the OpenID Connect specification
            // can be found here: http://openid.net/specs/openid-connect-core-1_0.html#StandardClaims
            await context.RespondAsync(new GetUserInfoResult(claims));
        }
    }
}