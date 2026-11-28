using Hagalaz.Game.Abstractions.Authorization;

namespace Hagalaz.Services.GameWorld.Services.Model
{
    public record HydratedClaims
    {
        public string UserName { get; init; } = default!;
        public string DisplayName { get; init; } = default!;
        public string? PreviousDisplayName { get; init; }
        public Permission Permissions { get; init; }
        public DateTimeOffset LastLogin { get; init; }
    }
}
