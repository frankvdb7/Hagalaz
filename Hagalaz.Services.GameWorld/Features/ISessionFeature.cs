using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Services.GameWorld.Features
{
    public interface ISessionFeature
    {
        public IGameSession Session { get; init; }
    }
}
