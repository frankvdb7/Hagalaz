using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Abstractions.Builders.Movement
{
    public interface IMovementBuild
    {
        IForceMovement Build();
    }
}