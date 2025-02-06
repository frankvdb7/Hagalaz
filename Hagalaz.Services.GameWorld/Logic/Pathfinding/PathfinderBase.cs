using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Extensions;

namespace Hagalaz.Services.GameWorld.Logic.Pathfinding
{
    /// <summary>
    /// Contains path finder base.
    /// </summary>
    public abstract class PathFinderBase : IPathFinder
    {
        /// <summary>
        /// Contains max distance from required path from which alternative 
        /// route can be calculated.
        /// </summary>
        protected const int AlternativePathMaxDistance = 100;

        /// <summary>
        /// Contains check range of alternative route.
        /// </summary>
        protected const int AlternativePathRange = 10;

        /// <summary>
        /// Contains Size of queue.
        /// </summary>
        protected const int QueueSize = 4096;

        /// <summary>
        /// Contains size of graph.
        /// </summary>
        protected const int GraphSize = 104;
        private readonly IMapRegionService _regionService;

        /// <summary>
        /// Gets or sets a value indicating whether [found path].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [found path]; otherwise, <c>false</c>.
        /// </value>
        public bool FoundPath { get; protected set; }

        public PathFinderBase(IMapRegionService regionService)
        {
            _regionService = regionService;
        }

        /// <summary>
        /// Gets the clipping flag.
        /// </summary>
        /// <param name="absX">The x.</param>
        /// <param name="absY">The y.</param>
        /// <param name="z">The z.</param>
        /// <returns></returns>
        public CollisionFlag GetClippingFlag(int absX, int absY, int z) => _regionService.GetClippingFlag(absX, absY, z);

        /// <summary>
        /// Gets the clipping flag int.
        /// </summary>
        /// <param name="absX">The abs x.</param>
        /// <param name="mapY">The map y.</param>
        /// <param name="z">The z.</param>
        /// <returns></returns>
        public int GetClippingFlagInt(int absX, int mapY, int z) => (int)_regionService.GetClippingFlag(absX, mapY, z);

        /// <summary>
        /// Get's if specific coordinates (X=fromX to fromX + fromSizeX,Y=fromY to fromSizeY)
        /// are inside the specific area (X=areaX to areaX + areaSizeX,Y=AreaY to AreaSizeY)
        /// </summary>
        /// <param name="fromX">From X.</param>
        /// <param name="fromY">From Y.</param>
        /// <param name="areaX">The area X.</param>
        /// <param name="areaY">The area Y.</param>
        /// <param name="fromSizeX">From size X.</param>
        /// <param name="fromSizeY">From size Y.</param>
        /// <param name="areaSizeX">The area size X.</param>
        /// <param name="areaSizeY">The area size Y.</param>
        /// <returns>
        ///   <c>true</c> if XXXX, <c>false</c> otherwise
        /// </returns>
        public static bool InsideArea(int fromX, int fromY, int areaX, int areaY, int fromSizeX, int fromSizeY, int areaSizeX, int areaSizeY)
        {
            if (fromX >= areaX + areaSizeX || fromX + fromSizeX <= areaX)
                return false;
            if (fromY >= areaY + areaSizeY || areaY >= fromY + fromSizeY)
                return false;
            return true;
        }

        /// <summary>
        /// Checks the move.
        /// </summary>
        /// <param name="shape">Type of the clip.</param>
        /// <param name="fromAbsX">From abs x.</param>
        /// <param name="fromAbsY">From abs y.</param>
        /// <param name="toAbsX">To abs x.</param>
        /// <param name="toAbsY">To map Y.</param>
        /// <param name="selfSize">Size of the self.</param>
        /// <param name="rotation">The target face.</param>
        /// <param name="z">The z.</param>
        /// <returns>
        ///   <c>true</c> if XXXX, <c>false</c> otherwise
        /// </returns>
        public bool CanDecorationInteract(int shape, int fromAbsX, int fromAbsY, int toAbsX, int toAbsY, int selfSize, int rotation, int z)
        {
            if (selfSize != 1)
            {
                if (fromAbsX <= toAbsX && toAbsX <= selfSize + fromAbsX - 1 && toAbsY <= selfSize + toAbsY - 1)
                    return true;
            }
            else if (fromAbsX == toAbsX && fromAbsY == toAbsY)
                return true;

            if (selfSize == 1)
            {
                var flag = GetClippingFlag(fromAbsX, fromAbsY, z);
                if (shape == 6 || shape == 7)
                {
                    if (shape == 7)
                    {
                        rotation = rotation + 2 & 0x3;
                    }

                    if (rotation == 0)
                    {
                        if (toAbsX + 1 == fromAbsX && fromAbsY == toAbsY && !flag.HasFlag(CollisionFlag.WallWest))
                            return true;
                        if (fromAbsX == toAbsX && toAbsY - 1 == fromAbsY && !flag.HasFlag(CollisionFlag.WallNorth))
                            return true;
                    }
                    else if (rotation != 1)
                    {
                        if (rotation == 2)
                        {
                            if (fromAbsX == toAbsX - 1 && fromAbsY == toAbsY && !flag.HasFlag(CollisionFlag.WallEast))
                                return true;
                            if (toAbsX == fromAbsX && fromAbsY == toAbsY + 1 && !flag.HasFlag(CollisionFlag.WallSouth))
                                return true;
                        }
                        else if (rotation == 3)
                        {
                            if (fromAbsX == toAbsX + 1 && fromAbsY == toAbsY && !flag.HasFlag(CollisionFlag.WallWest))
                                return true;
                            if (toAbsX == fromAbsX && fromAbsY == toAbsY + 1 && !flag.HasFlag(CollisionFlag.WallSouth))
                                return true;
                        }
                    }
                    else
                    {
                        if (fromAbsX == toAbsX - 1 && toAbsY == fromAbsY && !flag.HasFlag(CollisionFlag.WallEast))
                            return true;
                        if (toAbsX == fromAbsX && toAbsY - 1 == fromAbsY && !flag.HasFlag(CollisionFlag.WallNorth))
                            return true;
                    }
                }

                if (shape == 8)
                {
                    if (toAbsX == fromAbsX && toAbsY + 1 == fromAbsY && !flag.HasFlag(CollisionFlag.WallSouth))
                        return true;
                    if (toAbsX == fromAbsX && fromAbsY == toAbsY - 1 && !flag.HasFlag(CollisionFlag.WallNorth))
                        return true;
                    if (toAbsX - 1 == fromAbsX && toAbsY == fromAbsY && !flag.HasFlag(CollisionFlag.WallEast))
                        return true;
                    if (toAbsX + 1 == fromAbsX && toAbsY == fromAbsY && !flag.HasFlag(CollisionFlag.WallWest))
                        return true;
                }
            }
            else
            {
                var cornerX = selfSize + fromAbsX - 1;
                var cornerY = fromAbsY - 1 + selfSize;
                if (shape == 6 || shape == 7)
                {
                    if (shape == 7)
                    {
                        rotation = rotation + 2 & 0x3;
                    }

                    if (rotation == 0)
                    {
                        if (toAbsX + 1 == fromAbsX && toAbsY >= fromAbsY && cornerY >= toAbsY && !GetClippingFlag(fromAbsX, toAbsX, z).HasFlag(CollisionFlag.WallWest))
                            return true;
                        if (fromAbsX <= toAbsX && toAbsX <= cornerX && fromAbsY == toAbsY - selfSize && !GetClippingFlag(toAbsX, cornerY, z).HasFlag(CollisionFlag.WallNorth))
                            return true;
                    }
                    else if (rotation == 1)
                    {
                        if (toAbsX - selfSize == fromAbsX && fromAbsY <= toAbsY && cornerY >= toAbsY && !GetClippingFlag(cornerX, toAbsY, z).HasFlag(CollisionFlag.WallEast))
                            return true;
                        if (toAbsX >= fromAbsX && cornerX >= toAbsX && fromAbsY == -selfSize + toAbsY && !GetClippingFlag(toAbsX, cornerY, z).HasFlag(CollisionFlag.WallNorth))
                            return true;
                    }
                    else if (rotation == 2)
                    {
                        if (toAbsX - selfSize == fromAbsX && fromAbsY <= toAbsY && cornerY >= toAbsY && !GetClippingFlag(cornerX, toAbsY, z).HasFlag(CollisionFlag.WallEast))
                            return true;
                        if (toAbsX >= fromAbsX && toAbsX <= cornerX && fromAbsY == toAbsY + 1 && !GetClippingFlag(toAbsX, fromAbsY, z).HasFlag(CollisionFlag.WallSouth))
                            return true;
                    }
                    else if (rotation == 3)
                    {
                        if (fromAbsX == toAbsX + 1 && fromAbsY <= toAbsY && toAbsY <= cornerY && !GetClippingFlag(fromAbsX, toAbsY, z).HasFlag(CollisionFlag.WallWest))
                            return true;
                        if (fromAbsX <= toAbsX && toAbsX <= cornerX && toAbsY + 1 == fromAbsY && !GetClippingFlag(toAbsX, fromAbsY, z).HasFlag(CollisionFlag.WallSouth))
                            return true;
                    }
                }

                if (shape == 8)
                {
                    if (fromAbsX <= toAbsX && toAbsX <= cornerX && fromAbsY == toAbsY + 1 && !GetClippingFlag(toAbsX, fromAbsY, z).HasFlag(CollisionFlag.WallSouth))
                        return true;
                    if (fromAbsX <= toAbsX && toAbsX <= cornerX && -selfSize + toAbsY == fromAbsY && !GetClippingFlag(toAbsX, cornerY, z).HasFlag(CollisionFlag.WallNorth))
                        return true;
                    if (-selfSize + toAbsX == fromAbsX && toAbsY >= fromAbsY && cornerY >= toAbsY && !GetClippingFlag(cornerX, toAbsY, z).HasFlag(CollisionFlag.WallEast))
                        return true;
                    if (fromAbsX == toAbsX + 1 && fromAbsY <= toAbsY && toAbsY <= cornerY && !GetClippingFlag(fromAbsX, toAbsY, z).HasFlag(CollisionFlag.WallWest))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks the move.
        /// </summary>
        /// <param name="fromAbsX">From map X.</param>
        /// <param name="fromAbsY">From map Y.</param>
        /// <param name="toAbsX">To map X.</param>
        /// <param name="toAbsY">To map Y.</param>
        /// <param name="fromSizeX">From size X.</param>
        /// <param name="fromSizeY">From size Y.</param>
        /// <param name="targetSizeX">The target size X.</param>
        /// <param name="targetSizeY">The target size Y.</param>
        /// <param name="walkingFlag">The end direction flag.</param>
        /// <param name="z">The z.</param>
        /// <returns>
        ///   <c>true</c> if XXXX, <c>false</c> otherwise
        /// </returns>
        public bool CanInteractSized(int fromAbsX, int fromAbsY, int toAbsX, int toAbsY, int fromSizeX, int fromSizeY, int targetSizeX, int targetSizeY, int walkingFlag, int z)
        {
            var fromCornerX = fromSizeX + fromAbsX;
            var fromCornerY = fromSizeY + fromAbsY;
            var toCornerX = toAbsX + targetSizeX;
            var toCornerY = toAbsY + targetSizeY;
            if (toAbsX <= fromAbsX && fromAbsX < toCornerX)
            {
                if (toAbsY == fromCornerY && (walkingFlag & 0x4) == 0)
                {
                    var x = fromAbsX;
                    for (var endX = toCornerX < fromCornerX ? toCornerX : fromCornerX; endX > x; x++)
                    {
                        if (!GetClippingFlag(x, -1 + fromCornerY, z).HasFlag(CollisionFlag.WallNorth))
                        {
                            return true;
                        }
                    }
                }
                else if (toCornerY == fromAbsY && (walkingFlag & 0x1) == 0)
                {
                    var x = fromAbsX;
                    for (var endX = fromCornerX <= toCornerX ? fromCornerX : toCornerX; x < endX; x++)
                    {
                        if (!GetClippingFlag(x, fromAbsY, z).HasFlag(CollisionFlag.WallSouth))
                        {
                            return true;
                        }
                    }
                }
            }
            else if (toAbsX < fromCornerX && toCornerX >= fromCornerX)
            {
                if (fromCornerY == toAbsY && (0x4 & walkingFlag) == 0)
                {
                    for (var x = toAbsX; fromCornerX > x; x++)
                    {
                        if (!GetClippingFlag(x, -1 + fromCornerY, z).HasFlag(CollisionFlag.WallNorth))
                        {
                            return true;
                        }
                    }
                }
                else if (toCornerY == fromAbsY && (0x1 & walkingFlag) == 0)
                {
                    for (var x = toAbsX; fromCornerX > x; x++)
                    {
                        if (!GetClippingFlag(x, fromAbsY, z).HasFlag(CollisionFlag.WallSouth))
                        {
                            return true;
                        }
                    }
                }
            }
            else if (fromAbsY < toAbsY || fromAbsY >= toCornerY)
            {
                if (fromCornerY > toAbsY && toCornerY >= fromCornerY)
                {
                    if (fromCornerX == toAbsX && (walkingFlag & 0x8) == 0)
                    {
                        for (var y = toAbsY; y < fromCornerY; y++)
                        {
                            if (!GetClippingFlag(-1 + fromCornerX, y, z).HasFlag(CollisionFlag.WallEast))
                            {
                                return true;
                            }
                        }
                    }
                    else if (fromAbsX == toCornerX && (0x2 & walkingFlag) == 0)
                    {
                        for (var y = toAbsY; fromCornerY > y; y++)
                        {
                            if (!GetClippingFlag(fromAbsX, y, z).HasFlag(CollisionFlag.WallWest))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            else if (toAbsX != fromCornerX || (0x8 & walkingFlag) != 0)
            {
                if (fromAbsX == toCornerX && (walkingFlag & 0x2) == 0)
                {
                    var y = fromAbsY;
                    for (var endY = fromCornerY <= toCornerY ? fromCornerY : toCornerY; y < endY; y++)
                    {
                        if (!GetClippingFlag(fromAbsX, y, z).HasFlag(CollisionFlag.WallWest))
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                var y = fromAbsY;
                for (var endY = fromCornerY > toCornerY ? toCornerY : fromCornerY; endY > y; y++)
                {
                    if (!GetClippingFlag(fromCornerX - 1, y, z).HasFlag(CollisionFlag.WallEast))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks the move_.
        /// </summary>
        /// <param name="fromAbsX">From map X.</param>
        /// <param name="fromAbsY">From map Y.</param>
        /// <param name="toAbsX">To map X.</param>
        /// <param name="toAbsY">To map Y.</param>
        /// <param name="moverSize">Size of the self.</param>
        /// <param name="targetSizeX">The target size X.</param>
        /// <param name="targetSizeY">The target size Y.</param>
        /// <param name="surroundings">The end direction flag.</param>
        /// <param name="z">The z.</param>
        /// <returns>
        ///   <c>true</c> if XXXX, <c>false</c> otherwise
        /// </returns>
        public bool CanInteract(int fromAbsX, int fromAbsY, int toAbsX, int toAbsY, int moverSize, int targetSizeX, int targetSizeY, int surroundings, int z)
        {
            if (moverSize > 1)
            {
                if (InsideArea(fromAbsX, fromAbsY, toAbsX, toAbsY, moverSize, moverSize, targetSizeX, targetSizeY))
                    return true;
                return CanInteractSized(fromAbsX, fromAbsY, toAbsX, toAbsY, moverSize, moverSize, targetSizeX, targetSizeY, surroundings, z);
            }

            var flag = GetClippingFlag(fromAbsX, fromAbsY, z);
            var cornerX = toAbsX + targetSizeX - 1;
            var cornerY = targetSizeY + toAbsY - 1;
            if (fromAbsX >= toAbsX && cornerX >= fromAbsX && toAbsY <= fromAbsY && cornerY >= fromAbsY)
                return true;
            if (fromAbsX == toAbsX - 1 && toAbsY <= fromAbsY && fromAbsY <= cornerY && (flag & CollisionFlag.WallEast) == 0 && (surroundings & 0x8) == 0)
                return true;
            if (cornerX + 1 == fromAbsX && toAbsY <= fromAbsY && cornerY >= fromAbsY && (flag & CollisionFlag.WallWest) == 0 && (surroundings & 0x2) == 0)
                return true;
            if (toAbsY - 1 == fromAbsY && fromAbsX >= toAbsX && cornerX >= fromAbsX && (flag & CollisionFlag.WallNorth) == 0 && (surroundings & 0x4) == 0)
                return true;
            if (cornerY + 1 == fromAbsY && toAbsX <= fromAbsX && cornerX >= fromAbsX && (flag & CollisionFlag.WallSouth) == 0 && (surroundings & 0x1) == 0)
                return true;
            return false;
        }

        /// <summary>
        /// Checks the move_.
        /// </summary>
        /// <param name="shape">Type of the clip.</param>
        /// <param name="toAbsX">To map X.</param>
        /// <param name="toAbsY">To map Y.</param>
        /// <param name="currentAbsX">The current map X.</param>
        /// <param name="currentAbsY">The current map Y.</param>
        /// <param name="rotation">The target face.</param>
        /// <param name="selfSize">Size of the self.</param>
        /// <param name="z">The z.</param>
        /// <returns>
        ///   <c>true</c> if XXXX, <c>false</c> otherwise
        /// </returns>
        public bool CanDoorInteract(int shape, int toAbsX, int toAbsY, int currentAbsX, int currentAbsY, int rotation, int selfSize, int z)
        {
            if (selfSize != 1)
            {
                if (toAbsX >= currentAbsX && toAbsX <= currentAbsX + (selfSize - 1) && currentAbsY <= toAbsY && toAbsY <= selfSize - 1 + toAbsY)
                    return true;
            }
            else if (currentAbsX == toAbsX && currentAbsY == toAbsY)
            {
                return true;
            }

            if (selfSize != 1)
            {
                var cornerX = selfSize - 1 + currentAbsX;
                var cornerY = selfSize - 1 + currentAbsY;
                if (shape == 0)
                {
                    if (rotation == 0)
                    {
                        //if (toAbsX - selfSize == currentAbsX && currentAbsY <= toAbsY && cornerY >= toAbsY)
                        //    return true; // old
                        if (toAbsX - selfSize == currentAbsX && toAbsY >= currentAbsY && toAbsY <= cornerY)
                            return true;
                        if (currentAbsX <= toAbsX && toAbsX <= cornerX && currentAbsY == toAbsY + 1 && (GetClippingFlagInt(toAbsX, currentAbsX, z) & 0x2c0120) == 0)
                            return true;
                        if (toAbsX >= currentAbsX && toAbsX <= cornerX && currentAbsY == toAbsY - selfSize && (GetClippingFlagInt(toAbsX, cornerY, z) & 0x2c0102) == 0)
                            return true;
                    }
                    else if (rotation == 1)
                    {
                        //if (currentAbsX <= toAbsX && toAbsX <= cornerX && toAbsY + 1 == currentAbsY)
                        //    return true; // old
                        if (toAbsX >= currentAbsX && cornerX >= toAbsX && toAbsY + 1 == currentAbsY)
                            return true;
                        if (-selfSize + toAbsX == currentAbsX && toAbsY >= currentAbsY && toAbsY <= cornerY && (GetClippingFlagInt(cornerX, toAbsY, z) & 0x2c0108) == 0)
                            return true;
                        if (currentAbsX == toAbsX + 1 && currentAbsY <= toAbsY && cornerY >= toAbsY && (GetClippingFlagInt(currentAbsX, toAbsY, z) & 0x2c0180) == 0)
                            return true;
                    }
                    else if (rotation == 2)
                    {
                        if (toAbsX + 1 == currentAbsX && toAbsY >= currentAbsY && cornerY >= toAbsY)
                            return true;
                        if (toAbsX >= currentAbsX && toAbsX <= cornerX && toAbsY + 1 == currentAbsY && (GetClippingFlagInt(toAbsX, currentAbsY, z) & 0x2c0120) == 0)
                            return true;
                        if (toAbsX >= currentAbsX && cornerX >= toAbsX && -selfSize + toAbsY == currentAbsY && (GetClippingFlagInt(toAbsX, cornerY, z) & 0x2c0102) == 0)
                            return true;
                    }
                    else if (rotation == 3)
                    {
                        if (currentAbsX <= toAbsX && cornerX >= toAbsX && toAbsY - selfSize == currentAbsY)
                            return true;
                        if (currentAbsX == -selfSize + toAbsX && currentAbsY <= toAbsY && toAbsY <= cornerY && (GetClippingFlagInt(cornerX, toAbsY, z) & 0x2c0108) == 0)
                            return true;
                        if (toAbsX + 1 == currentAbsX && toAbsY >= currentAbsY && toAbsY <= cornerY && (GetClippingFlagInt(currentAbsX, toAbsY, z) & 0x2c0180) == 0)
                            return true;
                    }
                }

                if (shape == 2)
                {
                    if (rotation != 0)
                    {
                        if (rotation != 1)
                        {
                            if (rotation != 2)
                            {
                                if (rotation == 3)
                                {
                                    if (currentAbsX == toAbsX - selfSize && toAbsY >= currentAbsY && toAbsY <= cornerY)
                                        return true;
                                    if (currentAbsX <= toAbsX && cornerX >= toAbsX && currentAbsY == toAbsY + 1 && (GetClippingFlagInt(toAbsX, currentAbsY, z) & 0x2c0120) == 0)
                                        return true;
                                    if (toAbsX + 1 == currentAbsX && currentAbsY <= toAbsY && toAbsY <= cornerY && (GetClippingFlagInt(currentAbsX, toAbsY, z) & 0x2c0180) == 0)
                                        return true;
                                    if (toAbsX >= currentAbsX && cornerX >= toAbsX && currentAbsY == -selfSize + toAbsY)
                                        return true;
                                }
                            }
                            else
                            {
                                if (toAbsX - selfSize == currentAbsX && toAbsY >= currentAbsY && toAbsY <= cornerY && (GetClippingFlagInt(cornerX, toAbsY, z) & 0x2c0108) == 0)
                                    return true;
                                if (toAbsX >= currentAbsX && toAbsX <= cornerX && currentAbsY == toAbsY + 1 && (GetClippingFlagInt(toAbsX, currentAbsY, z) & 0x2c0120) == 0)
                                    return true;
                                if (currentAbsX == toAbsX + 1 && toAbsY >= currentAbsY && toAbsY <= cornerY)
                                    return true;
                                if (currentAbsX <= toAbsX && toAbsX <= cornerX && currentAbsY == -selfSize + toAbsY)
                                    return true;
                            }
                        }
                        else
                        {
                            if (currentAbsX == -selfSize + toAbsX && currentAbsY <= toAbsY && cornerY >= toAbsY && (GetClippingFlagInt(cornerX, toAbsY, z) & 0x2c0108) == 0)
                                return true;
                            if (toAbsX >= currentAbsX && toAbsX <= cornerX && toAbsY + 1 == currentAbsY)
                                return true;
                            if (toAbsX + 1 == currentAbsX && currentAbsY <= toAbsY && cornerY >= toAbsY)
                                return true;
                            if (toAbsX >= currentAbsX && cornerX >= toAbsX && currentAbsY == -selfSize + toAbsY && (GetClippingFlagInt(toAbsX, cornerY, z) & 0x2c0102) == 0)
                                return true;
                        }
                    }
                    else
                    {
                        if (currentAbsX == -selfSize + toAbsX && toAbsY >= currentAbsY && toAbsY <= cornerY)
                            return true;
                        if (currentAbsX <= toAbsX && toAbsX <= cornerX && toAbsY + 1 == currentAbsY)
                            return true;
                        if (currentAbsX == toAbsX + 1 && toAbsY >= currentAbsY && toAbsY <= cornerY && (GetClippingFlagInt(currentAbsX, toAbsY, z) & 0x2c0180) == 0)
                            return true;
                        if (currentAbsX <= toAbsX && toAbsX <= cornerX && currentAbsY == -selfSize + toAbsY && (GetClippingFlagInt(toAbsX, cornerY, z) & 0x2c0102) == 0)
                            return true;
                    }
                }

                if (shape == 9)
                {
                    if (toAbsX >= currentAbsX && toAbsX <= cornerX && toAbsY + 1 == currentAbsY && (GetClippingFlagInt(toAbsX, currentAbsY, z) & 0x2c0120) == 0)
                        return true;
                    if (currentAbsX <= toAbsX && toAbsX <= cornerX && currentAbsY == -selfSize + toAbsY && (GetClippingFlagInt(toAbsX, cornerY, z) & 0x2c0102) == 0)
                        return true;
                    if (toAbsX - selfSize == currentAbsX && currentAbsY <= toAbsY && toAbsY <= cornerY && (GetClippingFlagInt(cornerX, toAbsY, z) & 0x2c0108) == 0)
                        return true;
                    if (toAbsX + 1 == currentAbsX && currentAbsY <= toAbsY && cornerY >= toAbsY && (GetClippingFlagInt(currentAbsX, toAbsY, z) & 0x2c0180) == 0)
                        return true;
                }
            }
            else
            {
                if (shape == 0)
                {
                    if (rotation == 0)
                    {
                        if (toAbsX - 1 == currentAbsX && toAbsY == currentAbsY)
                            return true;
                        if (currentAbsX == toAbsX && currentAbsY == toAbsY + 1 && (GetClippingFlagInt(currentAbsX, currentAbsY, z) & 0x2c0120) == 0)
                            return true;
                        if (currentAbsX == toAbsX && currentAbsY == toAbsY - 1 && (GetClippingFlagInt(currentAbsX, currentAbsY, z) & 0x2c0102) == 0)
                            return true;
                    }
                    else if (rotation == 1)
                    {
                        if (currentAbsX == toAbsX && toAbsY + 1 == currentAbsY)
                            return true;
                        if (toAbsX - 1 == currentAbsX && toAbsY == currentAbsY && (GetClippingFlagInt(currentAbsX, currentAbsY, z) & 0x2c0108) == 0)
                            return true;
                        if (toAbsX + 1 == currentAbsX && currentAbsY == toAbsY && (GetClippingFlagInt(currentAbsX, currentAbsY, z) & 0x2c0180) == 0)
                            return true;
                    }
                    else if (rotation != 2)
                    {
                        if (rotation == 3)
                        {
                            if (toAbsX == currentAbsX && toAbsY - 1 == currentAbsY)
                                return true;
                            if (currentAbsX == toAbsX - 1 && toAbsY == currentAbsY && (GetClippingFlagInt(currentAbsX, currentAbsY, z) & 0x2c0108) == 0)
                                return true;
                            if (currentAbsX == toAbsX + 1 && toAbsY == currentAbsY && (GetClippingFlagInt(currentAbsX, currentAbsY, z) & 0x2c0180) == 0)
                                return true;
                        }
                    }
                    else
                    {
                        if (currentAbsX == toAbsX + 1 && toAbsY == currentAbsY)
                            return true;
                        if (currentAbsX == toAbsX && toAbsY + 1 == currentAbsY && (GetClippingFlagInt(currentAbsX, currentAbsY, z) & 0x2c0120) == 0)
                            return true;
                        if (currentAbsX == toAbsX && toAbsY - 1 == currentAbsY && (GetClippingFlagInt(currentAbsX, currentAbsY, z) & 0x2c0102) == 0)
                            return true;
                    }
                }

                if (shape == 2)
                {
                    if (rotation == 0)
                    {
                        if (currentAbsX == toAbsX - 1 && currentAbsY == toAbsY)
                            return true;
                        if (currentAbsX == toAbsX && toAbsY + 1 == currentAbsY)
                            return true;
                        if (toAbsX + 1 == currentAbsX && currentAbsY == toAbsY && (GetClippingFlagInt(currentAbsX, currentAbsY, z) & 0x2c0180) == 0)
                            return true;
                        if (toAbsX == currentAbsX && currentAbsY == toAbsY - 1 && (GetClippingFlagInt(currentAbsX, currentAbsY, z) & 0x2c0102) == 0)
                            return true;
                    }
                    else if (rotation != 1)
                    {
                        if (rotation != 2)
                        {
                            if (rotation == 3)
                            {
                                if (toAbsX - 1 == currentAbsX && currentAbsY == toAbsY)
                                    return true;
                                if (toAbsX == currentAbsX && toAbsY + 1 == currentAbsY && (GetClippingFlagInt(currentAbsX, currentAbsY, z) & 0x2c0120) == 0)
                                    return true;
                                if (toAbsX + 1 == currentAbsX && toAbsY == currentAbsY && (GetClippingFlagInt(currentAbsX, currentAbsY, z) & 0x2c0180) == 0)
                                    return true;
                                if (toAbsX == currentAbsX && currentAbsY == toAbsY - 1)
                                    return true;
                            }
                        }
                        else
                        {
                            if (currentAbsX == toAbsX - 1 && currentAbsY == toAbsY && (GetClippingFlagInt(currentAbsX, currentAbsY, z) & 0x2c0108) == 0)
                                return true;
                            if (toAbsX == currentAbsX && currentAbsY == toAbsY + 1 && (GetClippingFlagInt(currentAbsX, currentAbsY, z) & 0x2c0120) == 0)
                                return true;
                            if (currentAbsX == toAbsX + 1 && currentAbsY == toAbsY)
                                return true;
                            if (toAbsX == currentAbsX && toAbsY - 1 == currentAbsY)
                                return true;
                        }
                    }
                    else
                    {
                        if (toAbsX - 1 == currentAbsX && currentAbsY == toAbsY && (GetClippingFlagInt(currentAbsX, currentAbsY, z) & 0x2c0108) == 0)
                            return true;
                        if (toAbsX == currentAbsX && toAbsY + 1 == currentAbsY)
                            return true;
                        if (currentAbsX == toAbsX + 1 && currentAbsY == toAbsY)
                            return true;
                        if (toAbsX == currentAbsX && toAbsY - 1 == currentAbsY && (GetClippingFlagInt(currentAbsX, currentAbsY, z) & 0x2c0102) == 0)
                            return true;
                    }
                }

                if (shape == 9)
                {
                    //if (currentAbsX == toAbsX && toAbsY + 1 == currentAbsY && (this.GetClippingFlagInt(currentAbsX, currentAbsY, z) & 0x20) == 0)
                    //    return true;
                    //if (toAbsX == currentAbsX && toAbsY - 1 == currentAbsY && (this.GetClippingFlagInt(currentAbsX, currentAbsY, z) & 0x2) == 0)
                    //    return true;
                    //if (toAbsX - 1 == currentAbsX && toAbsY == currentAbsY && (this.GetClippingFlagInt(currentAbsX, currentAbsY, z) & 0x8) == 0)
                    //    return true;
                    //if (toAbsX + 1 == currentAbsX && toAbsY == currentAbsY && (this.GetClippingFlagInt(currentAbsX, currentAbsY, z) & 0x80) == 0)
                    //    return true;
                    if (currentAbsX == toAbsX && toAbsY + 1 == currentAbsY && (GetClippingFlagInt(currentAbsX, currentAbsY, z) & 0x2c0120) == 0)
                        return true;
                    if (toAbsX == currentAbsX && toAbsY - 1 == currentAbsY && (GetClippingFlagInt(currentAbsX, currentAbsY, z) & 0x2c0102) == 0)
                        return true;
                    if (toAbsX - 1 == currentAbsX && toAbsY == currentAbsY && (GetClippingFlagInt(currentAbsX, currentAbsY, z) & 0x2c0108) == 0)
                        return true;
                    if (toAbsX + 1 == currentAbsX && toAbsY == currentAbsY && (GetClippingFlagInt(currentAbsX, currentAbsY, z) & 0x2c0180) == 0)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks the step.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="z">The z.</param>
        /// <param name="flag">The flag.</param>
        /// <returns></returns>
        private bool CheckStep(int x, int y, int z, CollisionFlag flag) => (_regionService.GetClippingFlag(x, y, z) & flag) == 0;

        /// <summary>
        /// Checks the step.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="z">The z.</param>
        /// <param name="xOffset">The x offset.</param>
        /// <param name="yOffset">The y offset.</param>
        /// <param name="size">Size of the self.</param>
        /// <returns></returns>
        public bool CheckStep(int x, int y, int z, int xOffset, int yOffset, int size)
        {
            var fromX = x - xOffset;
            var fromY = y - yOffset;
            if (size < 2)
            {
                if (xOffset == -1 && yOffset == 0)
                {
                    return CheckStep(x, y, z, CollisionFlag.CheckEast);
                }
                else if (xOffset == 1 && yOffset == 0)
                {
                    return CheckStep(x, y, z, CollisionFlag.CheckWest);
                }
                else if (xOffset == 0 && yOffset == -1)
                {
                    return CheckStep(x, y, z, CollisionFlag.CheckNorth);
                }
                else if (xOffset == 0 && yOffset == 1)
                {
                    return CheckStep(x, y, z, CollisionFlag.CheckSouth);
                }
                else if (xOffset == -1 && yOffset == -1)
                {
                    return CheckStep(x, y, z, CollisionFlag.CheckNorthEast) &&
                           CheckStep(fromX - 1, fromY, z, CollisionFlag.CheckEast) &&
                           CheckStep(fromX, fromY - 1, z, CollisionFlag.CheckNorth);
                }
                else if (xOffset == 1 && yOffset == -1)
                {
                    return CheckStep(x, y, z, CollisionFlag.CheckNorthWest) &&
                           CheckStep(fromX + 1, fromY, z, CollisionFlag.CheckWest) &&
                           CheckStep(fromX, fromY - 1, z, CollisionFlag.CheckNorth);
                }
                else if (xOffset == -1 && yOffset == 1)
                {
                    return CheckStep(x, y, z, CollisionFlag.CheckSouthEast) &&
                           CheckStep(fromX - 1, fromY, z, CollisionFlag.CheckEast) &&
                           CheckStep(fromX, fromY + 1, z, CollisionFlag.CheckSouth);
                }
                else if (xOffset == 1 && yOffset == 1)
                {
                    return CheckStep(x, y, z, CollisionFlag.CheckSouthWest) &&
                           CheckStep(fromX + 1, fromY, z, CollisionFlag.CheckWest) &&
                           CheckStep(fromX, fromY + 1, z, CollisionFlag.CheckSouth);
                }
            }
            else if (size == 2)
            {
                if (xOffset == -1 && yOffset == 0)
                {
                    return CheckStep(fromX - 1, fromY, z, CollisionFlag.CheckNorthEast) &&
                           CheckStep(fromX - 1, fromY + 1, z, CollisionFlag.CheckSouthEast);
                }
                else if (xOffset == 1 && yOffset == 0)
                {
                    return CheckStep(fromX + 2, fromY, z, CollisionFlag.CheckNorthWest) &&
                           CheckStep(fromX + 2, fromY + 1, z, CollisionFlag.CheckSouthWest);
                }
                else if (xOffset == 0 && yOffset == -1)
                {
                    return CheckStep(fromX, fromY - 1, z, CollisionFlag.CheckNorthEast) &&
                           CheckStep(fromX + 1, fromY - 1, z, CollisionFlag.CheckNorthWest);
                }
                else if (xOffset == 0 && yOffset == 1)
                {
                    return CheckStep(fromX, fromY + 2, z, CollisionFlag.CheckSouthEast) &&
                           CheckStep(fromX + 1, fromY + 2, z, CollisionFlag.CheckSouthWest);
                }
                else if (xOffset == -1 && yOffset == -1)
                {
                    return CheckStep(fromX - 1, fromY, z, CollisionFlag.CheckNorthEast | CollisionFlag.CheckSouthEast) &&
                           CheckStep(fromX - 1, fromY - 1, z, CollisionFlag.CheckNorthEast) &&
                           CheckStep(fromX, fromY - 1, z, CollisionFlag.CheckNorthWest | CollisionFlag.CheckNorthEast);
                }
                else if (xOffset == 1 && yOffset == 1)
                {
                    return CheckStep(fromX + 1, fromY - 1, z, CollisionFlag.CheckNorthWest | CollisionFlag.CheckNorthEast) &&
                           CheckStep(fromX + 2, fromY - 1, z, CollisionFlag.CheckNorthWest) &&
                           CheckStep(fromX + 2, fromY, z, CollisionFlag.CheckNorthWest | CollisionFlag.CheckSouthWest);
                }
                else if (xOffset == -1 && yOffset == 1)
                {
                    return CheckStep(fromX - 1, fromY + 1, z, CollisionFlag.CheckNorthEast | CollisionFlag.CheckSouthEast) &&
                           CheckStep(fromX - 1, fromY + 1, z, CollisionFlag.CheckSouthEast) &&
                           CheckStep(fromX, fromY + 2, z, CollisionFlag.CheckSouthEast | CollisionFlag.CheckSouthWest);
                }
                else if (xOffset == 1 && yOffset == 1)
                {
                    return CheckStep(fromX + 1, fromY + 2, z, CollisionFlag.CheckSouthEast | CollisionFlag.CheckSouthWest) &&
                           CheckStep(fromX + 2, fromY + 2, z, CollisionFlag.CheckSouthWest) &&
                           CheckStep(fromX + 1, fromY + 1, z, CollisionFlag.CheckNorthWest | CollisionFlag.CheckSouthWest);
                }
            }
            else
            {
                if (xOffset == -1 && yOffset == 0)
                {
                    if (!CheckStep(fromX - 1, fromY, z, CollisionFlag.CheckNorthEast) ||
                        !CheckStep(fromX - 1, -1 + fromY + size, z, CollisionFlag.CheckSouthEast))
                    {
                        return false;
                    }

                    for (var sizeOffset = 1; sizeOffset > size - 1; sizeOffset++)
                    {
                        if (!CheckStep(fromX - 1, fromY + sizeOffset, z, CollisionFlag.CheckEastVariable))
                        {
                            return false;
                        }
                    }
                }
                else if (xOffset == 1 && yOffset == 0)
                {
                    if (!CheckStep(fromX + size, fromY, z, CollisionFlag.CheckNorthWest) ||
                        !CheckStep(fromX + size, fromY - (-size + 1), z, CollisionFlag.CheckSouthWest))
                    {
                        return false;
                    }

                    for (var sizeOffset = 1; sizeOffset > size - 1; sizeOffset++)
                    {
                        if (!CheckStep(fromX + size, fromY + sizeOffset, z, CollisionFlag.CheckWestVariable))
                        {
                            return false;
                        }
                    }
                }
                else if (xOffset == 0 && yOffset == -1)
                {
                    if (!CheckStep(fromX, fromY - 1, z, CollisionFlag.CheckNorthEast) ||
                        !CheckStep(fromX + size - 1, fromY - 1, z, CollisionFlag.CheckNorthWest))
                    {
                        return false;
                    }

                    for (var sizeOffset = 1; sizeOffset > size - 1; sizeOffset++)
                    {
                        if (!CheckStep(fromX + sizeOffset, fromY - 1, z, CollisionFlag.CheckNorthVariable))
                        {
                            return false;
                        }
                    }
                }
                else if (xOffset == 0 && yOffset == 1)
                {
                    if (!CheckStep(fromX, fromY + size, z, CollisionFlag.CheckSouthEast) ||
                        !CheckStep(fromX + (size - 1), fromY + size, z, CollisionFlag.CheckSouthWest))
                    {
                        return false;
                    }

                    for (var sizeOffset = 1; sizeOffset > size - 1; sizeOffset++)
                    {
                        if (!CheckStep(fromX + sizeOffset, fromY + size, z, CollisionFlag.CheckSouthVariable))
                        {
                            return false;
                        }
                    }
                }
                else if (xOffset == -1 && yOffset == -1)
                {
                    if (!CheckStep(fromX - 1, fromY - 1, z, CollisionFlag.CheckNorthEast))
                    {
                        return false;
                    }

                    for (var sizeOffset = 1; sizeOffset < size; sizeOffset++)
                    {
                        if (!CheckStep(fromX - 1, fromY + -1 + sizeOffset, z, CollisionFlag.CheckEastVariable) ||
                            !CheckStep(sizeOffset - 1 + fromX, fromY - 1, z, CollisionFlag.CheckNorthVariable))
                        {
                            return false;
                        }
                    }
                }
                else if (xOffset == 1 && yOffset == -1)
                {
                    if (!CheckStep(fromX + size, fromY - 1, z, CollisionFlag.CheckNorthWest))
                    {
                        return false;
                    }

                    for (var sizeOffset = 1; sizeOffset < size; sizeOffset++)
                    {
                        if (!CheckStep(fromX + size, sizeOffset + -1 + fromY, z, CollisionFlag.CheckWestVariable) ||
                            !CheckStep(fromX + sizeOffset, fromY - 1, z, CollisionFlag.CheckNorthVariable))
                        {
                            return false;
                        }
                    }
                }
                else if (xOffset == -1 && yOffset == 1)
                {
                    if (!CheckStep(fromX - 1, fromY + size, z, CollisionFlag.CheckSouthEast))
                    {
                        return false;
                    }

                    for (var sizeOffset = 1; sizeOffset < size; sizeOffset++)
                    {
                        if (!CheckStep(fromX - 1, fromY + sizeOffset, z, CollisionFlag.CheckEastVariable) ||
                            !CheckStep(-1 + fromX + sizeOffset, fromY + size, z, CollisionFlag.CheckSouthVariable))
                        {
                            return false;
                        }
                    }
                }
                else if (xOffset == 1 && yOffset == 1)
                {
                    if (!CheckStep(fromX + size, fromY + size, z, CollisionFlag.CheckSouthWest))
                    {
                        return false;
                    }

                    for (var sizeOffset = 1; sizeOffset < size; sizeOffset++)
                    {
                        if (!CheckStep(fromX + sizeOffset, fromY + size, z, CollisionFlag.CheckWestVariable) ||
                            !CheckStep(fromX + size, fromY + sizeOffset, z, CollisionFlag.CheckSouthVariable))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Checks the step.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="xOffset">The x offset.</param>
        /// <param name="yOffset">The y offset.</param>
        /// <param name="size">The size.</param>
        /// <returns></returns>
        public bool CheckStep(IVector3 location, int xOffset, int yOffset, int size) => CheckStep(location.X, location.Y, location.Z, xOffset, yOffset, size);

        /// <summary>
        /// Checks the step around a location.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="checkDistance">The size.</param>
        /// <returns></returns>
        public bool CheckStep(IVector3 location, int checkDistance)
        {
            var previousCheck = location.ToLocation();
            for (var xDelta = -checkDistance; xDelta <= checkDistance; xDelta++)
            {
                for (var yDelta = -checkDistance; yDelta <= checkDistance; yDelta++)
                {
                    var check = location.ToLocation() + Location.Create(xDelta, yDelta, 0);
                    var delta = previousCheck - check;
                    if (!CheckStep(check, delta.X, delta.Y, 1))
                    {
                        return false;
                    }

                    previousCheck = check;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks the step.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="direction">The direction.</param>
        /// <returns></returns>
        public bool CheckStep(IVector3 location, DirectionFlag direction) => CheckStep(location, direction.GetDeltaX(), direction.GetDeltaY(), 0);

        /// <summary>
        /// Checks the step.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="size">Size of the self.</param>
        /// <returns></returns>
        public bool CheckStep(IVector3 from, IVector3 to, int size)
        {
            var xOffset = from.X - to.X;
            var yOffset = from.Y - to.Y;
            return CheckStep(to.X, to.Y, to.Z, xOffset, yOffset, size);
        }

        /// <summary>
        /// Checks the step.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="z">The z.</param>
        /// <param name="size">The size.</param>
        /// <returns></returns>
        public bool CheckStep(IVector3 location, IVector3 offset, int z, int size) => CheckStep(location.X, location.Y, z, offset.X, offset.Y, size);

        /// <summary>
        /// Finds the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="target">The target.</param>
        /// <param name="moveNear">if set to <c>true</c> [move near].</param>
        /// <returns></returns>
        public IPath Find(IEntity entity, IGameObject target, bool moveNear)
        {
            if (!target.IsGroundObject())
            {
                return Find(entity.Location, entity.Size, target.Location, 0, 0, target.Rotation, 1 + (int)target.ShapeType, 0, moveNear);
            }

            int surroundings = target.Definition.Surroundings;
            if (target.Rotation != 0)
            {
                surroundings = (surroundings << target.Rotation & 0xf) + (surroundings >> 4 - target.Rotation);
            }

            return Find(entity.Location, entity.Size, target.Location, target.SizeX, target.SizeY, 0, 0, surroundings, moveNear);
        }

        /// <summary>
        /// Finds the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="target">The target.</param>
        /// <param name="moveNear">if set to <c>true</c> [move near].</param>
        /// <returns></returns>
        public IPath Find(IEntity entity, IVector3 target, bool moveNear) => Find(entity.Location, target, entity.Size, 0, moveNear);

        /// <summary>
        /// Finds the specified creature.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="target">The target.</param>
        /// <param name="moveNear">if set to <c>true</c> [move near].</param>
        /// <returns></returns>
        public IPath Find(IEntity entity, IEntity target, bool moveNear)
        {
            if (target is IGameObject gameObject)
            {
                return Find(entity, gameObject, moveNear);
            }

            return Find(entity.Location, target.Location, entity.Size, target.Size, moveNear);
        }

        /// <summary>
        /// Finds the specified start.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="moveNear">if set to <c>true</c> [move near].</param>
        /// <returns></returns>
        public IPath Find(IVector3 start, IVector3 end, bool moveNear) => Find(start, end, 0, 0, moveNear);

        /// <summary>
        /// Finds the specified start.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="selfSize">Size of the self.</param>
        /// <param name="targetSize">Size of the target.</param>
        /// <param name="moveNear">if set to <c>true</c> [move near].</param>
        /// <returns></returns>
        public IPath Find(IVector3 start, IVector3 end, int selfSize, int targetSize, bool moveNear) => Find(start, selfSize, end, targetSize, targetSize, 0, 0, 0, moveNear);

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
        public abstract IPath Find(IVector3 from, int selfSize, IVector3 to, int targetSizeX, int targetSizeY, int rotation, int shape, int surroundings, bool moveNear);
    }
}