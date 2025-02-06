using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Services.GameWorld.Logic.Pathfinding
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="PathFinderBase" />
    public class DumbPathFinder : PathFinderBase, ISimplePathFinder
    {
        public DumbPathFinder(IMapRegionService regionService) : base(regionService) { }

        /// <summary>
        /// Finds a path from the location to the end location.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="selfSize">Size of the self.</param>
        /// <param name="to">To.</param>
        /// <param name="targetSizeX">The target size x.</param>
        /// <param name="targetSizeY">The target size y.</param>
        /// <param name="rotation">The rotation.</param>
        /// <param name="shape">The shape.</param>
        /// <param name="surroundings">The surroundings.</param>
        /// <param name="moveNear">if set to <c>true</c> [move near].</param>
        /// <returns></returns>
        public override IPath Find(
            IVector3 from, int selfSize, IVector3 to, int targetSizeX, int targetSizeY, int rotation, int shape, int surroundings, bool moveNear)
        {
            var x = from.X;
            var y = from.Y;
            var z = from.Z;

            var path = new Path
            {
                Successful = true
            };
            while (x != to.X || y != to.Y)
            {
                if (QueueSize <= ++path.Steps)
                {
                    return path;
                }
                var direction = DirectionHelper.GetDirection(x, y, to.X, to.Y);
                if (selfSize < 2)
                {
                    CheckSingleTraversal(path, direction, ref x, ref y, ref z);
                }
                else if (selfSize == 2)
                {
                    CheckDoubleTraversal(path, direction, ref x, ref y, ref z);
                }
                else
                {
                    CheckVariableTraversal(path, direction, selfSize, ref x, ref y, ref z);
                }

                if (!path.Successful)
                {
                    path.MovedNear = x != from.X || y != from.Y;
                    break;
                }
            }

            return path;
        }

        public void CheckSingleTraversal(Path path, DirectionFlag direction, ref int x, ref int y, ref int z)
        {
            if (direction == DirectionFlag.North)
            {
                if (!IsTraversable(x, y + 1, z, CollisionFlag.TraversableNorthBlocked))
                {
                    path.Successful = false;
                    return;
                }

                y++;
            }
            else if (direction == DirectionFlag.NorthEast)
            {
                if (!IsTraversable(x + 1, y, z, CollisionFlag.TraversableEastBlocked) ||
                    !IsTraversable(x, y + 1, z, CollisionFlag.TraversableNorthBlocked) ||
                    !IsTraversable(x + 1, y + 1, z, CollisionFlag.TraversableNorthWestBlocked))
                {
                    path.Successful = false;
                    return;
                }

                x++;
                y++;
            }
            else if (direction == DirectionFlag.East)
            {
                if (!IsTraversable(x + 1, y, z, CollisionFlag.TraversableEastBlocked))
                {
                    path.Successful = false;
                    return;
                }

                x++;
            }
            else if (direction == DirectionFlag.SouthEast)
            {
                if (!IsTraversable(x + 1, y, z, CollisionFlag.TraversableEastBlocked) ||
                    !IsTraversable(x, y - 1, z, CollisionFlag.TraversableSouthBlocked) ||
                    !IsTraversable(x + 1, y - 1, z, CollisionFlag.TraversableSouthEastBlocked))
                {
                    path.Successful = false;
                    return;
                }

                x++;
                y--;
            }
            else if (direction == DirectionFlag.South)
            {
                if (!IsTraversable(x, y - 1, z, CollisionFlag.TraversableSouthBlocked))
                {
                    path.Successful = false;
                    return;
                }

                y--;
            }
            else if (direction == DirectionFlag.SouthWest)
            {
                if (!IsTraversable(x - 1, y, z, CollisionFlag.TraversableWestBlocked) ||
                    !IsTraversable(x, y - 1, z, CollisionFlag.TraversableSouthBlocked) ||
                    !IsTraversable(x - 1, y - 1, z, CollisionFlag.TraversableSouthWestBlocked))
                {
                    path.Successful = false;
                    return;
                }

                x--;
                y--;
            }
            else if (direction == DirectionFlag.West)
            {
                if (!IsTraversable(x - 1, y, z, CollisionFlag.TraversableWestBlocked))
                {
                    path.Successful = false;
                    return;
                }

                x--;
            }
            else if (direction == DirectionFlag.NorthWest)
            {
                if (!IsTraversable(x - 1, y, z, CollisionFlag.TraversableWestBlocked) ||
                    !IsTraversable(x, y + 1, z, CollisionFlag.TraversableNorthBlocked) ||
                    !IsTraversable(x - 1, y + 1, z, CollisionFlag.TraversableNorthWestBlocked))
                {
                    path.Successful = false;
                    return;
                }

                x--;
                y++;
            }

            path.Add(Location.Create(x, y, z));
        }

        private void CheckDoubleTraversal(Path path, DirectionFlag direction, ref int x, ref int y, ref int z)
        {
            if (direction == DirectionFlag.North)
            {
                if (!IsTraversable(x, y + 2, z, CollisionFlag.TraversableNorthWestBlocked) ||
                    !IsTraversable(x + 1, y + 2, z, CollisionFlag.TraversableNorthEastBlocked))
                {
                    path.Successful = false;
                    return;
                }

                y++;
            }
            else if (direction == DirectionFlag.NorthEast)
            {
                if (!IsTraversable(x + 1, y + 2, z, CollisionFlag.TraversableNorthWestBlocked) ||
                    !IsTraversable(x + 2, y + 2, z, CollisionFlag.TraversableNorthEastBlocked) ||
                    !IsTraversable(x + 2, y + 1, z, CollisionFlag.TraversableSouthEastBlocked))
                {
                    path.Successful = false;
                    return;
                }

                x++;
                y++;
            }
            else if (direction == DirectionFlag.East)
            {
                if (!IsTraversable(x + 2, y, z, CollisionFlag.TraversableSouthEastBlocked) ||
                    !IsTraversable(x + 2, y + 1, z, CollisionFlag.TraversableNorthEastBlocked))
                {
                    path.Successful = false;
                    return;
                }

                x++;
            }
            else if (direction == DirectionFlag.SouthEast)
            {
                if (!IsTraversable(x + 1, y - 1, z, CollisionFlag.TraversableSouthWestBlocked) ||
                    !IsTraversable(x + 2, y, z, CollisionFlag.TraversableNorthEastBlocked) ||
                    !IsTraversable(x + 2, y - 1, z, CollisionFlag.TraversableSouthEastBlocked))
                {
                    path.Successful = false;
                    return;
                }

                x++;
                y--;
            }
            else if (direction == DirectionFlag.South)
            {
                if (!IsTraversable(x, y - 1, z, CollisionFlag.TraversableSouthWestBlocked) ||
                    !IsTraversable(x + 1, y - 1, z, CollisionFlag.TraversableSouthEastBlocked))
                {
                    path.Successful = false;
                    return;
                }

                y--;
            }
            else if (direction == DirectionFlag.SouthWest)
            {
                if (!IsTraversable(x - 1, y - 1, z, CollisionFlag.TraversableSouthWestBlocked) ||
                    !IsTraversable(x - 1, y, z, CollisionFlag.TraversableNorthWestBlocked) ||
                    !IsTraversable(x, y - 1, z, CollisionFlag.TraversableSouthEastBlocked))
                {
                    path.Successful = false;
                    return;
                }

                x--;
                y++;
            }
            else if (direction == DirectionFlag.West)
            {
                if (!IsTraversable(x - 1, y, z, CollisionFlag.TraversableSouthWestBlocked) ||
                    !IsTraversable(x - 1, y + 1, z, CollisionFlag.TraversableNorthWestBlocked))
                {
                    path.Successful = false;
                    return;
                }

                x--;
            }
            else if (direction == DirectionFlag.NorthWest)
            {
                if (!IsTraversable(x - 1, y + 1, z, CollisionFlag.TraversableSouthWestBlocked) ||
                    !IsTraversable(x - 1, y + 2, z, CollisionFlag.TraversableNorthWestBlocked) ||
                    !IsTraversable(x, y + 2, z, CollisionFlag.TraversableNorthEastBlocked))
                {
                    path.Successful = false;
                    return;
                }

                x--;
                y++;
            }

            path.Add(Location.Create(x, y, z));
        }

        private void CheckVariableTraversal(Path path, DirectionFlag direction, int size, ref int x, ref int y, ref int z)
        {
            int xEnd = x + size - 1;
            int yEnd = y + size - 1;

            if (direction == DirectionFlag.North)
            {
                for (var i = x; i <= xEnd; i++)
                {
                    if (!IsTraversable(i, y + size, z, CollisionFlag.TraversableNorthBlocked))
                    {
                        path.Successful = false;
                        return;
                    }
                }

                y++;
            }
            else if (direction == DirectionFlag.NorthEast)
            {
                for (var i = x; i <= xEnd + 1; i++)
                {
                    if (!IsTraversable(i, y + size, z, CollisionFlag.TraversableNorthBlocked))
                    {
                        path.Successful = false;
                        return;
                    }
                }

                for (var j = y; j <= yEnd + 1; j++)
                {
                    if (!IsTraversable(x + size, j, z, CollisionFlag.TraversableEastBlocked))
                    {
                        path.Successful = false;
                        return;
                    }
                }

                if (!IsTraversable(x + size, y + size, z, CollisionFlag.TraversableNorthEastBlocked))
                {
                    path.Successful = false;
                    return;
                }

                x++;
                y++;
            }
            else if (direction == DirectionFlag.East)
            {
                for (var j = y; j <= yEnd; j++)
                {
                    if (!IsTraversable(x + size, j, z, CollisionFlag.TraversableEastBlocked))
                    {
                        path.Successful = false;
                        return;
                    }
                }

                x++;
            }
            else if (direction == DirectionFlag.SouthEast)
            {
                for (var i = x; i <= xEnd + 1; i++)
                {
                    if (!IsTraversable(i, y - 1, z, CollisionFlag.TraversableSouthBlocked))
                    {
                        path.Successful = false;
                        return;
                    }
                }

                for (var j = y - 1; j <= yEnd; j++)
                {
                    if (!IsTraversable(x + size, j, z, CollisionFlag.TraversableEastBlocked))
                    {
                        path.Successful = false;
                        return;
                    }
                }

                if (!IsTraversable(x + size, y - 1, z, CollisionFlag.TraversableSouthEastBlocked))
                {
                    path.Successful = false;
                    return;
                }

                x++;
                y--;
            }
            else if (direction == DirectionFlag.South)
            {
                for (var i = x; i <= xEnd; i++)
                {
                    if (!IsTraversable(i, y - 1, z, CollisionFlag.TraversableSouthBlocked))
                    {
                        path.Successful = false;
                        return;
                    }
                }

                y--;
            }
            else if (direction == DirectionFlag.SouthWest)
            {
                for (var i = x - 1; i <= xEnd; i++)
                {
                    if (!IsTraversable(i, y - 1, z, CollisionFlag.TraversableSouthBlocked))
                    {
                        path.Successful = false;
                        return;
                    }
                }

                for (int j = y - 1; j <= yEnd; j++)
                {
                    if (!IsTraversable(x - 1, j, z, CollisionFlag.TraversableWestBlocked))
                    {
                        path.Successful = false;
                        return;
                    }
                }

                if (!IsTraversable(x - 1, y - 1, z, CollisionFlag.TraversableSouthWestBlocked))
                {
                    path.Successful = false;
                    return;
                }

                x--;
                y--;
            }
            else if (direction == DirectionFlag.West)
            {
                for (var j = y; j <= yEnd; j++)
                {
                    if (!IsTraversable(x - 1, j, z, CollisionFlag.TraversableWestBlocked))
                    {
                        path.Successful = false;
                        return;
                    }
                }

                x--;
            }
            else if (direction == DirectionFlag.NorthWest)
            {
                for (var i = x - 1; i <= xEnd; i++)
                {
                    if (!IsTraversable(i, y + size, z, CollisionFlag.TraversableNorthBlocked))
                    {
                        path.Successful = false;
                        return;
                    }
                }

                for (var j = y; j <= yEnd + 1; j++)
                {
                    if (!IsTraversable(x - 1, j, z, CollisionFlag.TraversableWestBlocked))
                    {
                        path.Successful = false;
                        return;
                    }
                }

                if (!IsTraversable(x - 1, y + size, z, CollisionFlag.TraversableNorthWestBlocked))
                {
                    path.Successful = false;
                    return;
                }

                x--;
                y++;
            }

            path.Add(Location.Create(x, y, z));
        }


        /// <summary>
        /// Determines whether the specified x is traversable.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="z">The z.</param>
        /// <param name="flag">The flag.</param>
        /// <returns>
        ///   <c>true</c> if the specified x is traversable; otherwise, <c>false</c>.
        /// </returns>
        private bool IsTraversable(int x, int y, int z, CollisionFlag flag) => (GetClippingFlag(x, y, z) & flag) == 0;
    }
}