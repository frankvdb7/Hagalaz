using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Game.Abstractions.Builders.Movement
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a forced movement effect
    /// where optional parameters like direction and speed can be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IMovementBuilder"/>.
    /// It also inherits from <see cref="IMovementBuild"/>, allowing the build process to be finalized at this stage.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IMovementOptional : IMovementBuild
    {
        /// <summary>
        /// Sets the direction the entity should face upon starting the movement.
        /// </summary>
        /// <param name="direction">The <see cref="FaceDirection"/> to face.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        public IMovementOptional WithFaceDirection(FaceDirection direction);

        /// <summary>
        /// Sets the direction of the movement itself.
        /// </summary>
        /// <param name="direction">The <see cref="DirectionFlag"/> indicating the direction of travel.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        public IMovementOptional WithDirection(DirectionFlag direction);

        /// <summary>
        /// Sets the speed of the movement from the start location.
        /// </summary>
        /// <param name="speed">The speed, where each unit typically corresponds to a 30ms interval (e.g., 1 = 30ms, 2 = 60ms).</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        public IMovementOptional WithStartSpeed(int speed);

        /// <summary>
        /// Sets the speed of the movement towards the end location.
        /// </summary>
        /// <param name="speed">The speed, where each unit typically corresponds to a 30ms interval (e.g., 1 = 30ms, 2 = 60ms).</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        public IMovementOptional WithEndSpeed(int speed);
    }
}