using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Services.GameWorld.Features
{
    public class SessionFeature : ISessionFeature
    {
        public required IGameSession Session  { get; init; }
    }
}
