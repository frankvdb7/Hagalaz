using System.Collections.Immutable;

namespace Hagalaz.Services.Authorization.Mediator.Commands
{
    public record PasswordGrantCommand(string Login, string Password, ImmutableArray<string> Scopes);
}