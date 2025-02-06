using System.Security.Claims;

namespace Hagalaz.Services.Authorization.Mediator.Commands
{
    public record GetUserInfoCommand(ClaimsPrincipal User);
}