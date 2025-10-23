using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Game.Extensions
{
    /// <summary>
    /// Provides extension methods for direction-related enums like <see cref="DirectionFlag"/>.
    /// </summary>
    public static class DirectionExtensions
    {
        /// <summary>
        /// Calculates the opposite of a given direction.
        /// </summary>
        /// <param name="direction">The direction to reverse.</param>
        /// <returns>The reversed direction, or <see cref="DirectionFlag.None"/> if the input direction is invalid.</returns>
        public static DirectionFlag Reverse(this DirectionFlag direction) =>
            direction switch
            {
                DirectionFlag.North => DirectionFlag.South,
                DirectionFlag.East => DirectionFlag.West,
                DirectionFlag.South => DirectionFlag.North,
                DirectionFlag.West => DirectionFlag.East,
                DirectionFlag.NorthEast => DirectionFlag.SouthWest,
                DirectionFlag.SouthEast => DirectionFlag.NorthWest,
                DirectionFlag.SouthWest => DirectionFlag.NorthEast,
                DirectionFlag.NorthWest => DirectionFlag.SouthEast,
                _ => DirectionFlag.None
            };

        /// <summary>
        /// Gets the change in the X-coordinate for a given direction.
        /// </summary>
        /// <param name="direction">The direction of movement.</param>
        /// <returns>-1 for West, 1 for East, and 0 otherwise.</returns>
        public static int GetDeltaX(this DirectionFlag direction)
        {
            if (direction == DirectionFlag.None)
            {
                return 0;
            }
            if ((direction & DirectionFlag.West) != 0)
                return -1;
            else if ((direction & DirectionFlag.East) != 0)
                return 1;
            return 0;
        }

        /// <summary>
        /// Gets the change in the Y-coordinate for a given direction.
        /// </summary>
        /// <param name="direction">The direction of movement.</param>
        /// <returns>-1 for South, 1 for North, and 0 otherwise.</returns>
        public static int GetDeltaY(this DirectionFlag direction)
        {
            if (direction == DirectionFlag.None)
            {
                return 0;
            }
            if ((direction & DirectionFlag.South) != 0)
                return -1;
            else if ((direction & DirectionFlag.North) != 0)
                return 1;
            return 0;
        }

        /// <summary>
        /// Converts a <see cref="DirectionFlag"/> to its corresponding <see cref="FaceDirection"/>.
        /// </summary>
        /// <param name="direction">The direction flag to convert.</param>
        /// <returns>The corresponding face direction.</returns>
        public static FaceDirection ToFaceDirection(this DirectionFlag direction) =>
            direction switch
            {
                DirectionFlag.North => FaceDirection.North,
                DirectionFlag.East => FaceDirection.East,
                DirectionFlag.South => FaceDirection.South,
                DirectionFlag.West => FaceDirection.West,
                DirectionFlag.NorthEast => FaceDirection.NorthEast,
                DirectionFlag.SouthEast => FaceDirection.SouthEast,
                DirectionFlag.SouthWest => FaceDirection.SouthWest,
                DirectionFlag.NorthWest => FaceDirection.NorthWest,
                _ => FaceDirection.None
            };
    }
}