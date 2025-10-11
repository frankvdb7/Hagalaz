namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// Defines the contract for a forced movement, which is a non-standard movement path for a creature,
    /// often used for special effects like knockbacks or cinematic sequences.
    /// </summary>
    public interface IForceMovement
    {
        /// <summary>
        /// Gets the starting location of the forced movement.
        /// </summary>
        ILocation StartLocation { get; }

        /// <summary>
        /// Gets the ending location of the forced movement.
        /// </summary>
        ILocation EndLocation { get; }

        /// <summary>
        /// Gets the speed of the movement towards the end location.
        /// </summary>
        int EndSpeed { get; }

        /// <summary>
        /// Gets the speed of the movement from the start location.
        /// </summary>
        int StartSpeed { get; }

        /// <summary>
        /// Gets the direction the creature should face during the movement.
        /// </summary>
         FaceDirection FaceDirection { get; }
    }
}
