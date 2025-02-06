using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Game.Abstractions.Builders.Movement
{
    public interface IMovementOptional : IMovementBuild
    {
        /// <summary>
        /// The direction to face when the movement starts.
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public IMovementOptional WithFaceDirection(FaceDirection direction);
        /// <summary>
        /// The direction to face when the movement starts.
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public IMovementOptional WithDirection(DirectionFlag direction);
        /// <summary>
        /// The speed towards the start location.
        /// </summary>
        /// <param name="speed">The speed to start (1 = 30ms, 2 = 60ms etc.).</param>
        /// <returns></returns>
        public IMovementOptional WithStartSpeed(int speed);
        /// <summary>
        /// The speed towards the end location.
        /// </summary>
        /// <param name="speed">The speed to end (1 = 30ms, 2 = 60ms etc.).</param>
        /// <returns></returns>
        public IMovementOptional WithEndSpeed(int speed);
    }
}