using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Services.Authorization.Mediator.Commands;
using Hagalaz.Services.Authorization.Model;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Hagalaz.Services.Authorization.Controllers
{
    [ApiController]
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    public class UserInfoController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IRequestClient<GetUserInfoCommand> _requestClientGetUserInfoCommand;

        public UserInfoController(IMediator mediator)
        {
            _mediator = mediator;
            _requestClientGetUserInfoCommand = _mediator.CreateRequestClient<GetUserInfoCommand>();
        }

        // GET: /api/connect/userinfo
        [HttpGet("~/connect/userinfo"), Produces("application/json")]
        public async Task<IActionResult> Userinfo()
        {
            var response = await _requestClientGetUserInfoCommand.GetResponse<GetUserInfoResult>(new GetUserInfoCommand(User));
            var result = response.Message;
            if (result.Claims == null)
            {
                return Challenge(authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The specified access token is bound to an account that no longer exists."
                    }));
            }
            
            return Ok(result.Claims);
        }
    }
}