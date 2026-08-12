using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Services.GameWorld.Logic.Pathfinding
{
    /// <summary>
    /// Traces projectile line of sight through map collision flags.
    /// </summary>
    public class ProjectilePathFinder : PathFinderBase, IProjectilePathFinder
    {
        private const CollisionFlag FullLineOfSightBlocker = CollisionFlag.ObjectBlock;

        public ProjectilePathFinder(IMapRegionService regionService) : base(regionService) { }

        /// <summary>
        /// Finds a projectile line of sight from the source tile to the target tile.
        /// </summary>
        public override IPath Find(
            IVector3 from, int selfSize, IVector3 to, int targetSizeX, int targetSizeY, int rotation, int shape, int surroundings, bool moveNear)
        {
            var path = new Path
            {
                Successful = from.Z == to.Z
            };

            if (!path.Successful || (from.X == to.X && from.Y == to.Y))
            {
                return path;
            }

            var deltaX = to.X - from.X;
            var deltaY = to.Y - from.Y;
            var xFlags = FullLineOfSightBlocker | (deltaX < 0 ? CollisionFlag.BlockedEast : CollisionFlag.BlockedWest);
            var yFlags = FullLineOfSightBlocker | (deltaY < 0 ? CollisionFlag.BlockedNorth : CollisionFlag.BlockedSouth);
            var diagonalFlag = GetDiagonalLineOfSightBlocker(deltaX, deltaY);

            var successful = Math.Abs(deltaX) > Math.Abs(deltaY)
                ? TraceXAxis(path, from.X, from.Y, from.Z, to.X, deltaX, deltaY, xFlags, yFlags, diagonalFlag)
                : TraceYAxis(path, from.X, from.Y, from.Z, to.Y, deltaX, deltaY, xFlags, yFlags, diagonalFlag);

            if (!successful)
            {
                path.Successful = false;
                path.MovedNear = path.Steps > 0;
                return path;
            }

            path.Add(Location.Create(to.X, to.Y, to.Z));
            return path;
        }

        private bool TraceXAxis(
            Path path, int fromX, int fromY, int z, int targetX, int deltaX, int deltaY, CollisionFlag xFlags, CollisionFlag yFlags, CollisionFlag diagonalFlag)
        {
            var x = fromX;
            var yBig = (fromY << 16) + 0x8000;
            if (deltaY < 0)
            {
                yBig--;
            }

            var slope = (deltaY << 16) / Math.Abs(deltaX);
            var direction = deltaX < 0 ? -1 : 1;

            while (x != targetX)
            {
                path.Steps++;
                x += direction;

                var y = GetTileCoordinate(yBig);
                if (!IsTraversable(x, y, z, xFlags))
                {
                    return false;
                }

                yBig += slope;
                var nextY = GetTileCoordinate(yBig);
                if (nextY != y && !IsTraversable(x, nextY, z, yFlags))
                {
                    return false;
                }

                if (nextY != y && IsDiagonalRay(deltaX, deltaY) && !IsTraversable(x, nextY, z, diagonalFlag))
                {
                    return false;
                }
            }

            return true;
        }

        private bool TraceYAxis(
            Path path, int fromX, int fromY, int z, int targetY, int deltaX, int deltaY, CollisionFlag xFlags, CollisionFlag yFlags, CollisionFlag diagonalFlag)
        {
            var y = fromY;
            var xBig = (fromX << 16) + 0x8000;
            if (deltaX < 0)
            {
                xBig--;
            }

            var slope = (deltaX << 16) / Math.Abs(deltaY);
            var direction = deltaY < 0 ? -1 : 1;

            while (y != targetY)
            {
                path.Steps++;
                y += direction;

                var x = GetTileCoordinate(xBig);
                if (!IsTraversable(x, y, z, yFlags))
                {
                    return false;
                }

                xBig += slope;
                var nextX = GetTileCoordinate(xBig);
                if (nextX != x && !IsTraversable(nextX, y, z, xFlags))
                {
                    return false;
                }

                if (nextX != x && IsDiagonalRay(deltaX, deltaY) && !IsTraversable(nextX, y, z, diagonalFlag))
                {
                    return false;
                }
            }

            return true;
        }

        private static int GetTileCoordinate(int fixedPointCoordinate) => (int)((uint)fixedPointCoordinate >> 16);

        private static bool IsDiagonalRay(int deltaX, int deltaY) => Math.Abs(deltaX) == Math.Abs(deltaY);

        private static CollisionFlag GetDiagonalLineOfSightBlocker(int deltaX, int deltaY)
        {
            if (deltaX < 0)
            {
                return deltaY < 0 ? CollisionFlag.BlockedNorthEast : CollisionFlag.BlockedSouthEast;
            }

            return deltaY < 0 ? CollisionFlag.BlockedNorthWest : CollisionFlag.BlockedSouthWest;
        }

        private bool IsTraversable(int x, int y, int z, CollisionFlag flags)
        {
            if ((GetClippingFlag(x, y, z) & flags) != 0)
            {
                return false;
            }

            return true;
        }
    }
}
