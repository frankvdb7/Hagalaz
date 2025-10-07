using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Game.Extensions
{
    public static class DirectionExtensions
    {
        /// <summary>
        /// Reverses the specified direction.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <returns></returns>
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
        /// Gets the delta x.
        /// </summary>
        /// <param name="direction">The flag.</param>
        /// <returns></returns>
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
        /// Gets the delta y.
        /// </summary>
        /// <param name="direction">The flag.</param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
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