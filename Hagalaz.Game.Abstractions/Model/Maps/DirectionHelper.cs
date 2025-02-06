namespace Hagalaz.Game.Abstractions.Model.Maps
{
    /// <summary>
    /// 
    /// </summary>
    public static class DirectionHelper
    {
        /// <summary>
        /// 
        /// </summary>
        public static readonly sbyte[] DirectionDeltaX =
        [
            0, 1, 0, -1, 1, 1, -1, -1
        ];

        /// <summary>
        /// 
        /// </summary>
        public static readonly sbyte[] DirectionDeltaY =
        [
            1, 0, -1, 0, 1, -1, 1, -1
        ];

        /// <summary>
        /// Three bits movement type
        /// [x + 1,y + 1] = Type
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
        /// Four bits movement type
        /// [x + 2,y + 2] = Type
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
        /// Contains region movement type
        /// [x + 1,y + 1] = Type
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
        /// NPC three bits movement type.
        /// [x + 1,y + 1] = Type
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
        /// Gets the type of the NPC movement.
        /// </summary>
        /// <param name="deltaX">The delta x.</param>
        /// <param name="deltaY">The delta y.</param>
        /// <returns></returns>
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
        /// Gets the direction flag of the npc face direction
        /// </summary>
        /// <param name="faceDirection"></param>
        /// <returns></returns>
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
        /// Gets the direction.
        /// </summary>
        /// <param name="fromX">From x.</param>
        /// <param name="fromY">From y.</param>
        /// <param name="toX">To x.</param>
        /// <param name="toY">To y.</param>
        /// <returns></returns>
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