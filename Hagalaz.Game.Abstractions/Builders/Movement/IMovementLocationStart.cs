using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.Movement
{
    public interface IMovementLocationStart
    {
        /// <summary>
        /// Location where movement starts.
        /// </summary>
        IMovementLocationEnd WithStart(ILocation location);
    }
}