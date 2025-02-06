using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.Movement
{
    public interface IMovementLocationEnd
    {
        /// <summary>
        /// Location where movement ends.
        /// </summary>
        IMovementOptional WithEnd(ILocation location);
    }
}