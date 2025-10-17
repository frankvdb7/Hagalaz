namespace Hagalaz.Game.Abstractions.Model.Maps
{
    /// <summary>
    /// Provides static helper methods and data for direction-related calculations.
    /// </summary>
    public static class DirectionHelper
    {
        /// <summary>
        /// An array of X-coordinate offsets corresponding to different directions.
        /// </summary>
        public static readonly sbyte[] DirectionDeltaX =
        [
            0, 1, 0, -1, 1, 1, -1, -1
        ];

        /// <summary>
        /// An array of Y-coordinate offsets corresponding to different directions.
        /// </summary>
        public static readonly sbyte[] DirectionDeltaY =
        [
            1, 0, -1, 0, 1, -1, 1, -1
        ];

        /// <summary>
        /// A lookup table for 3-bit movement types based on delta coordinates.
        /// [x + 1, y + 1] = Type
        /// </summary>
        public static readonly int[,] ThreeBitsMovementType = new[,]
        {
            {
                0, 3, 5
            },
            {
                1, -1, 6
            },
            {
                2, 4, 7
            }
        };

        /// <summary>
        /// A lookup table for 4-bit movement types based on delta coordinates.
        /// [x + 2, y + 2] = Type
        /// </summary>
        public static readonly int[,] FourBitsMovementType = new[,]
        {
            {
                0, 5, 7, 9, 11
            },
            {
                1, -1, -1, -1, 12
            },
            {
                2, -1, -1, -1, 13
            },
            {
                3, -1, -1, -1, 14
            },
            {
                4, 6, 8, 10, 15
            }
        };

        /// <summary>
        /// A lookup table for region-based movement types.
        /// [x + 1, y + 1] = Type
        /// </summary>
        public static readonly int[,] RegionMovementType = new[,]
        {
            {
                0, 3, 5
            },
            {
                1, -1, 6
            },
            {
                2, 4, 7
            }
        };

        /// <summary>
        /// A lookup table for NPC 3-bit movement types based on delta coordinates.
        /// [x + 1, y + 1] = Type
        /// </summary>
        public static readonly int[,] ThreeBitsNpcMovementType = new[,]
        {
            {
                5, 6, 7
            },
            {
                4, -1, 0
            },
            {
                3, 2, 1
            }
        };

        /// <summary>
        /// Gets the movement type ID for an NPC based on its change in coordinates.
        /// </summary>
        /// <param name="deltaX">The change in the X-coordinate.</param>
        /// <param name="deltaY">The change in the Y-coordinate.</param>
        /// <returns>The movement type ID, or -1 if the movement is invalid.</returns>
        public static int GetNpcMovementType(int deltaX, int deltaY)
        {
            if (deltaX == 0 && deltaY > 0)
                return 0;
            if (deltaX > 0 && deltaY > 0)
                return 1;
            if (deltaX > 0 && deltaY == 0)
                return 2;
            if (deltaX > 0 && deltaY < 0)
                return 3;
            if (deltaX == 0 && deltaY < 0)
                return 4;
            if (deltaX < 0 && deltaY < 0)
                return 5;
            if (deltaX < 0 && deltaY == 0)
                return 6;
            if (deltaX < 0 && deltaY > 0)
                return 7;
            return -1;
        }

        /// <summary>
        /// Gets the direction flag corresponding to an NPC's face direction ID.
        /// </summary>
        /// <param name="faceDirection">The face direction ID.</param>
        /// <returns>The corresponding <see cref="DirectionFlag"/>.</returns>
        public static DirectionFlag GetNpcFaceDirection(int faceDirection)
        {
            if (faceDirection == 0)
            {
                return DirectionFlag.North;
            }
            else if (faceDirection == 1)
            {
                return DirectionFlag.NorthEast;
            }
            else if (faceDirection == 2)
            {
                return DirectionFlag.East;
            }
            else if (faceDirection == 3)
            {
                return DirectionFlag.SouthEast;
            }
            else if (faceDirection == 4)
            {
                return DirectionFlag.South;
            }
            else if (faceDirection == 5)
            {
                return DirectionFlag.SouthWest;
            }
            else if (faceDirection == 6)
            {
                return DirectionFlag.West;
            }
            else if (faceDirection == 7)
            {
                return DirectionFlag.NorthWest;
            }
            return DirectionFlag.None;
        }

        /// <summary>
        /// Calculates the direction from a starting coordinate to a target coordinate.
        /// </summary>
        /// <param name="fromX">The starting X-coordinate.</param>
        /// <param name="fromY">The starting Y-coordinate.</param>
        /// <param name="toX">The target X-coordinate.</param>
        /// <param name="toY">The target Y-coordinate.</param>
        /// <returns>The calculated <see cref="DirectionFlag"/>.</returns>
        public static DirectionFlag GetDirection(int fromX, int fromY, int toX, int toY)
        {
            if (fromX == toX)
            {
                if (fromY > toY)
                {
                    return DirectionFlag.South;
                }
                else if (fromY < toY)
                {
                    return DirectionFlag.North;
                }
            }
            else if (fromY == toY)
            {
                if (fromX > toX)
                {
                    return DirectionFlag.West;
                }

                return DirectionFlag.East;
            }
            else
            {
                if (fromX < toX && fromY < toY)
                {
                    return DirectionFlag.NorthEast;
                }
                else if (fromX < toX && fromY > toY)
                {
                    return DirectionFlag.SouthEast;
                }
                else if (fromX > toX && fromY < toY)
                {
                    return DirectionFlag.NorthWest;
                }
                else if (fromX > toX && fromY > toY)
                {
                    return DirectionFlag.SouthWest;
                }
            }

            return DirectionFlag.None;
        }
    }
}