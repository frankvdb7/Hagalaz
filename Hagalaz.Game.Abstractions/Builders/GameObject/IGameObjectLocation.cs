using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.GameObject
{
    public interface IGameObjectLocation
    {
        public IGameObjectOptional WithLocation(ILocation location);
    }
}