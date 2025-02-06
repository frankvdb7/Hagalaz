using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Authorization.Mediator.Commands;
using Hagalaz.Services.Authorization.Model;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Hagalaz.Services.Authorization.Controllers
{
    [ApiController]
    public class AuthorizationController : Controller
    {
        private readonly IMediator _mediator;
        private readonly SignInManager<Character> _signInManager;
        private readonly IRequestClient<PasswordGrantCommand> _requestClientPasswordGrantCommand;
        private readonly IRequestClient<RefreshTokenGrantCommand> _requestClientRefreshTokenGrantCommand;

        public AuthorizationController(SignInManager<Character> signInManager, IMediator mediator)
        {
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _requestClientPasswordGrantCommand = _mediator.CreateRequestClient<PasswordGrantCommand>();
            _requestClientRefreshTokenGrantCommand = _mediator.CreateRequestClient<RefreshTokenGrantCommand>();
        }

        [HttpPost("~/connect/token"), Produces("application/json"), IgnoreAntiforgeryToken]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");
            if (request.IsPasswordGrantType())
            {
                if (request.Username == null || request.Password == null)
                {
                    throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");
                }
                var passwordGrantResponse = await _requestClientPasswordGrantCommand.GetResponse<PasswordGrantResult>(new PasswordGrantCommand(request.Username, request.Password, request.GetScopes()));
                var passwordGrantResult = passwordGrantResponse.Message;
                if (passwordGrantResult.Succeeded)
                {
                    return SignIn(passwordGrantResult.User!, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }

                AuthenticationProperties properties = null!;
                if (!passwordGrantResult.Succeeded)
                {
                    properties = new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.AccessDenied,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The provided credentials are invalid."
                    });
                }

                return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
            else if (request.IsRefreshTokenGrantType()) 
            { 
                var refreshTokenGrantResponse = await _requestClientRefreshTokenGrantCommand.GetResponse<RefreshTokenGrantResult>(new RefreshTokenGrantCommand());
                var refreshTokenGrantResult = refreshTokenGrantResponse.Message;

                if (refreshTokenGrantResult.Succeeded)
                {
                    return SignIn(refreshTokenGrantResult.User!, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }

                return Forbid(refreshTokenGrantResult.AuthenticationProperties!, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            throw new InvalidOperationException("The specified grant type is not implemented.");
        }

        [HttpGet("~/connect/logout"), HttpPost("~/connect/logout"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Ask ASP.NET Core Identity to delete the local and external cookies created
            // when the user agent is redirected from the external identity provider
            // after a successful authentication flow (e.g Google or Facebook).
            await _signInManager.SignOutAsync();

            // Returning a SignOutResult will ask OpenIddict to redirect the user agent
            // to the post_logout_redirect_uri specified by the client application.
            return SignOut(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
    }
}