using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Services.Model
{
    public record SignInRequest
    {
        public string Login { get; init; } = default!;
        public string Password { get; init; } = default!;
        public IGameClient GameClient { get; init; } = default!;
    }
}
