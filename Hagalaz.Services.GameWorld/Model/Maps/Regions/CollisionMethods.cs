using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Extensions;

namespace Hagalaz.Services.GameWorld.Model.Maps.Regions
{
    /// <summary>
    /// Contains clip related methods for region.
    /// </summary>
    public partial class MapRegion
    {
        /// <summary>
        /// Get's clip at specific location.
        /// Throws exception if location is invalid.
        /// </summary>
        /// <param name="localX">The x.</param>
        /// <param name="localY">The y.</param>
        /// <param name="z">The z.</param>
        /// <returns>System.Int32.</returns>
        public CollisionFlag GetCollision(int localX, int localY, int z) => _collision[z, localX, localY];

        /// <summary>
        /// Set's clip at specific location.
        /// Throws exception if location is invalid.
        /// </summary>
        /// <param name="localX">The x.</param>
        /// <param name="localY">The y.</param>
        /// <param name="z">The z.</param>
        /// <param name="value">The val.</param>
        public void SetCollision(int localX, int localY, int z, CollisionFlag value) => _collision[z, localX, localY] = value;

        /// <summary>
        /// Flag's clip with specific flag at specific location.
        /// Throws exception if location is invalid.
        /// </summary>
        /// <param name="localX">The x.</param>
        /// <param name="localY">The y.</param>
        /// <param name="z">The z.</param>
        /// <param name="flag">The flag.</param>
        public void FlagCollision(int localX, int localY, int z, CollisionFlag flag) => _collision[z, localX, localY] |= flag;

        /// <summary>
        /// UnFlag's clip with specific flag at specific location.
        /// Throws exception if location is invalid.
        /// </summary>
        /// <param name="localX">The x.</param>
        /// <param name="localY">The y.</param>
        /// <param name="z">The z.</param>
        /// <param name="flag">The flag.</param>
        public void UnFlagCollision(int localX, int localY, int z, CollisionFlag flag) => _collision[z, localX, localY] &= ~flag;

        /// <summary>
        /// Flag's decoration object.
        /// </summary>
        /// <param name="obj">The obj.</param>
        public void FlagFloorDecorationCollision(IGameObject obj)
        {
            if (obj.Definition.ClipType == 1)
            {
                _collision[obj.Location.Z, obj.Location.RegionLocalX, obj.Location.RegionLocalY] |= CollisionFlag.FloorDecorationBlock;
            }
        }

        /// <summary>
        /// UnFlag's decoration object.
        /// </summary>
        /// <param name="obj">The obj.</param>
        public void UnFlagFloorDecorationCollision(IGameObject obj) => _collision[obj.Location.Z, obj.Location.RegionLocalX, obj.Location.RegionLocalY] &= ~CollisionFlag.FloorDecorationBlock;

        /// <summary>
        /// Flag's standart game object.
        /// </summary>
        /// <param name="obj">The obj.</param>
        public void FlagStandardObject(IGameObject obj)
        {
            if (obj.Definition.ClipType == 0 && !obj.Definition.Gateway)
            {
                return;
            }

            var flag = CollisionFlag.ObjectTile;
            if (obj.Definition.Solid)
                flag |= CollisionFlag.ObjectBlock;
            if (!obj.Definition.Gateway)
                flag |= CollisionFlag.ObjectAllowRange;

            int sizeX;
            int sizeY;
            if (obj.Rotation == 1 || obj.Rotation == 3)
            {
                sizeX = obj.Definition.SizeY;
                sizeY = obj.Definition.SizeX;
            }
            else
            {
                sizeX = obj.Definition.SizeX;
                sizeY = obj.Definition.SizeY;
            }

            for (var xcalc = obj.Location.X; xcalc < obj.Location.X + sizeX; xcalc++)
                for (var ycalc = obj.Location.Y; ycalc < obj.Location.Y + sizeY; ycalc++)
                    _regionService.FlagCollision(Location.Create(xcalc, ycalc, obj.Location.Z, obj.Location.Dimension), flag);
        }

        /// <summary>
        /// UnFlag's standart game object.
        /// </summary>
        /// <param name="obj">The obj.</param>
        public void UnFlagStandardObject(IGameObject obj)
        {
            var flag = CollisionFlag.ObjectTile;
            if (obj.Definition.Solid)
                flag |= CollisionFlag.ObjectBlock;
            if (!obj.Definition.Gateway)
                flag |= CollisionFlag.ObjectAllowRange;

            int sizeX;
            int sizeY;
            if (obj.Rotation == 1 || obj.Rotation == 3)
            {
                sizeX = obj.Definition.SizeY;
                sizeY = obj.Definition.SizeX;
            }
            else
            {
                sizeX = obj.Definition.SizeX;
                sizeY = obj.Definition.SizeY;
            }

            for (int xcalc = obj.Location.X; xcalc < obj.Location.X + sizeX; xcalc++)
                for (int ycalc = obj.Location.Y; ycalc < obj.Location.Y + sizeY; ycalc++)
                    _regionService.UnFlagCollision(Location.Create(xcalc, ycalc, obj.Location.Z, obj.Location.Dimension), flag);
        }

        public void FlagWallObject(IGameObject obj)
        {
            if (obj.Definition.ClipType == 0 && !obj.Definition.Gateway)
            {
                return;
            }

            var x = obj.Location.X;
            var y = obj.Location.Y;
            var z = obj.Location.Z;

            var xm1Ym1 = Location.Create(x - 1, y - 1, z, obj.Location.Dimension);
            var xm1Y0 = Location.Create(x - 1, y, z, obj.Location.Dimension);
            var xm1Y1 = Location.Create(x - 1, y + 1, z, obj.Location.Dimension);
            var x0Ym1 = Location.Create(x, y - 1, z, obj.Location.Dimension);
            var x0Y0 = Location.Create(x, y, z, obj.Location.Dimension);
            var x0Y1 = Location.Create(x, y + 1, z, obj.Location.Dimension);
            var x1Ym1 = Location.Create(x + 1, y - 1, z, obj.Location.Dimension);
            var x1Y0 = Location.Create(x + 1, y, z, obj.Location.Dimension);
            var x1Y1 = Location.Create(x + 1, y + 1, z, obj.Location.Dimension);


            if (obj.ShapeType == ShapeType.Wall)
            {
                if (obj.Rotation == 0)
                {
                    _regionService.FlagCollision(x0Y0, CollisionFlag.WallWest);
                    _regionService.FlagCollision(xm1Y0, CollisionFlag.WallEast);
                }

                if (obj.Rotation == 1)
                {
                    _regionService.FlagCollision(x0Y0, CollisionFlag.WallNorth);
                    _regionService.FlagCollision(x0Y1, CollisionFlag.WallSouth);
                }

                if (obj.Rotation == 2)
                {
                    _regionService.FlagCollision(x0Y0, CollisionFlag.WallEast);
                    _regionService.FlagCollision(x1Y0, CollisionFlag.WallWest);
                }

                if (obj.Rotation == 3)
                {
                    _regionService.FlagCollision(x0Y0, CollisionFlag.WallSouth);
                    _regionService.FlagCollision(x0Ym1, CollisionFlag.WallNorth);
                }
            }

            if (obj.ShapeType == ShapeType.WallCornerDiagonal || obj.ShapeType == ShapeType.WallCorner)
            {
                if (obj.Rotation == 0)
                {
                    _regionService.FlagCollision(x0Y0, CollisionFlag.WallNorthWest);
                    _regionService.FlagCollision(xm1Y1, CollisionFlag.WallSouthEast);
                }

                if (obj.Rotation == 1)
                {
                    _regionService.FlagCollision(x0Y0, CollisionFlag.WallNorthEast);
                    _regionService.FlagCollision(x1Y1, CollisionFlag.WallSouthWest);
                }

                if (obj.Rotation == 2)
                {
                    _regionService.FlagCollision(x0Y0, CollisionFlag.WallSouthEast);
                    _regionService.FlagCollision(x1Ym1, CollisionFlag.WallNorthWest);
                }

                if (obj.Rotation == 3)
                {
                    _regionService.FlagCollision(x0Y0, CollisionFlag.WallSouthWest);
                    _regionService.FlagCollision(xm1Ym1, CollisionFlag.WallNorthEast);
                }
            }

            if (obj.ShapeType == ShapeType.UnfinishedWall)
            {
                if (obj.Rotation == 0)
                {
                    _regionService.FlagCollision(x0Y0, CollisionFlag.WallWest | CollisionFlag.WallNorth);
                    _regionService.FlagCollision(xm1Y0, CollisionFlag.WallEast);
                    _regionService.FlagCollision(x0Y1, CollisionFlag.WallSouth);
                }

                if (obj.Rotation == 1)
                {
                    _regionService.FlagCollision(x0Y0, CollisionFlag.WallNorth | CollisionFlag.WallEast);
                    _regionService.FlagCollision(x0Y1, CollisionFlag.WallSouth);
                    _regionService.FlagCollision(x1Y0, CollisionFlag.WallWest);
                }

                if (obj.Rotation == 2)
                {
                    _regionService.FlagCollision(x0Y0, CollisionFlag.WallSouth | CollisionFlag.WallEast);
                    _regionService.FlagCollision(x1Y0, CollisionFlag.WallWest);
                    _regionService.FlagCollision(x0Ym1, CollisionFlag.WallNorth);
                }

                if (obj.Rotation == 3)
                {
                    _regionService.FlagCollision(x0Y0, CollisionFlag.WallWest | CollisionFlag.WallSouth);
                    _regionService.FlagCollision(x0Ym1, CollisionFlag.WallNorth);
                    _regionService.FlagCollision(xm1Y0, CollisionFlag.WallEast);
                }
            }

            if (obj.Definition.Solid)
            {
                if (obj.ShapeType == ShapeType.Wall)
                {
                    if (obj.Rotation == 0)
                    {
                        _regionService.FlagCollision(x0Y0, CollisionFlag.BlockedWest);
                        _regionService.FlagCollision(xm1Y0, CollisionFlag.BlockedEast);
                    }

                    if (obj.Rotation == 1)
                    {
                        _regionService.FlagCollision(x0Y0, CollisionFlag.BlockedNorth);
                        _regionService.FlagCollision(x0Y1, CollisionFlag.BlockedSouth);
                    }

                    if (obj.Rotation == 2)
                    {
                        _regionService.FlagCollision(x0Y0, CollisionFlag.BlockedEast);
                        _regionService.FlagCollision(x1Y0, CollisionFlag.BlockedWest);
                    }

                    if (obj.Rotation == 3)
                    {
                        _regionService.FlagCollision(x0Y0, CollisionFlag.BlockedSouth);
                        _regionService.FlagCollision(x0Ym1, CollisionFlag.BlockedNorth);
                    }
                }

                if (obj.ShapeType == ShapeType.WallCornerDiagonal || obj.ShapeType == ShapeType.WallCorner)
                {
                    if (obj.Rotation == 0)
                    {
                        _regionService.FlagCollision(x0Y0, CollisionFlag.BlockedNorthWest);
                        _regionService.FlagCollision(xm1Y1, CollisionFlag.BlockedSouthEast);
                    }

                    if (obj.Rotation == 1)
                    {
                        _regionService.FlagCollision(x0Y0, CollisionFlag.BlockedSouthEast);
                        _regionService.FlagCollision(x1Y1, CollisionFlag.BlockedSouthWest);
                    }

                    if (obj.Rotation == 2)
                    {
                        _regionService.FlagCollision(x0Y0, CollisionFlag.BlockedSouthEast);
                        _regionService.FlagCollision(x1Ym1, CollisionFlag.BlockedNorthWest);
                    }

                    if (obj.Rotation == 3)
                    {
                        _regionService.FlagCollision(x0Y0, CollisionFlag.BlockedSouthWest);
                        _regionService.FlagCollision(xm1Ym1, CollisionFlag.BlockedSouthEast);
                    }
                }

                if (obj.ShapeType == ShapeType.UnfinishedWall)
                {
                    if (obj.Rotation == 0)
                    {
                        _regionService.FlagCollision(x0Y0, CollisionFlag.BlockedWest | CollisionFlag.BlockedNorth);
                        _regionService.FlagCollision(xm1Y0, CollisionFlag.BlockedEast);
                        _regionService.FlagCollision(x0Y1, CollisionFlag.BlockedSouth);
                    }

                    if (obj.Rotation == 1)
                    {
                        _regionService.FlagCollision(x0Y0, CollisionFlag.BlockedEast | CollisionFlag.BlockedNorth);
                        _regionService.FlagCollision(x0Y1, CollisionFlag.BlockedSouth);
                        _regionService.FlagCollision(x1Y0, CollisionFlag.BlockedWest);
                    }

                    if (obj.Rotation == 2)
                    {
                        _regionService.FlagCollision(x0Y0, CollisionFlag.BlockedSouth | CollisionFlag.BlockedEast);
                        _regionService.FlagCollision(x1Y0, CollisionFlag.BlockedWest);
                        _regionService.FlagCollision(x0Ym1, CollisionFlag.BlockedNorth);
                    }

                    if (obj.Rotation == 3)
                    {
                        _regionService.FlagCollision(x0Y0, CollisionFlag.BlockedWest | CollisionFlag.BlockedSouth);
                        _regionService.FlagCollision(x0Ym1, CollisionFlag.BlockedNorth);
                        _regionService.FlagCollision(xm1Y0, CollisionFlag.BlockedEast);
                    }
                }
            }

            if (!obj.Definition.Gateway)
            {
                if (obj.ShapeType == ShapeType.Wall)
                {
                    if (obj.Rotation == 0)
                    {
                        _regionService.FlagCollision(x0Y0, CollisionFlag.WallAllowRangeWest);
                        _regionService.FlagCollision(xm1Y0, CollisionFlag.WallAllowRangeEast);
                    }

                    if (obj.Rotation == 1)
                    {
                        _regionService.FlagCollision(x0Y0, CollisionFlag.WallAllowRangeNorth);
                        _regionService.FlagCollision(x0Y1, CollisionFlag.WallAllowRangeSouth);
                    }

                    if (obj.Rotation == 2)
                    {
                        _regionService.FlagCollision(x0Y0, CollisionFlag.WallAllowRangeEast);
                        _regionService.FlagCollision(x1Y0, CollisionFlag.WallAllowRangeWest);
                    }

                    if (obj.Rotation == 3)
                    {
                        _regionService.FlagCollision(x0Y0, CollisionFlag.WallAllowRangeSouth);
                        _regionService.FlagCollision(x0Ym1, CollisionFlag.WallAllowRangeNorth);
                    }
                }

                if (obj.ShapeType == ShapeType.WallCornerDiagonal || obj.ShapeType == ShapeType.WallCorner)
                {
                    if (obj.Rotation == 0)
                    {
                        _regionService.FlagCollision(x0Y0, CollisionFlag.WallAllowRangeNorthWest);
                        _regionService.FlagCollision(xm1Y1, CollisionFlag.WallAllowRangeSouthEast);
                    }

                    if (obj.Rotation == 1)
                    {
                        _regionService.FlagCollision(x0Y0, CollisionFlag.WallAllowRangeNorthEast);
                        _regionService.FlagCollision(x1Y1, CollisionFlag.WallAllowRangeSouthWest);
                    }

                    if (obj.Rotation == 2)
                    {
                        _regionService.FlagCollision(x0Y0, CollisionFlag.WallAllowRangeSouthEast);
                        _regionService.FlagCollision(x1Ym1, CollisionFlag.WallAllowRangeNorthWest);
                    }

                    if (obj.Rotation == 3)
                    {
                        _regionService.FlagCollision(x0Y0, CollisionFlag.WallAllowRangeSouthWest);
                        _regionService.FlagCollision(xm1Ym1, CollisionFlag.WallAllowRangeNorthEast);
                    }
                }

                if (obj.ShapeType == ShapeType.UnfinishedWall)
                {
                    if (obj.Rotation == 0)
                    {
                        _regionService.FlagCollision(x0Y0, CollisionFlag.WallAllowRangeSouth | CollisionFlag.WallAllowRangeEast);
                        _regionService.FlagCollision(xm1Y0, CollisionFlag.WallAllowRangeEast);
                        _regionService.FlagCollision(x0Y1, CollisionFlag.WallAllowRangeSouth);
                    }

                    if (obj.Rotation == 1)
                    {
                        _regionService.FlagCollision(x0Y0, CollisionFlag.WallAllowRangeNorth | CollisionFlag.WallAllowRangeEast);
                        _regionService.FlagCollision(x0Y1, CollisionFlag.WallAllowRangeSouth);
                        _regionService.FlagCollision(x1Y0, CollisionFlag.WallAllowRangeWest);
                    }

                    if (obj.Rotation == 2)
                    {
                        _regionService.FlagCollision(x0Y0, CollisionFlag.WallAllowRangeSouth | CollisionFlag.WallAllowRangeEast);
                        _regionService.FlagCollision(x1Y0, CollisionFlag.WallAllowRangeWest);
                        _regionService.FlagCollision(x0Ym1, CollisionFlag.WallAllowRangeNorth);
                    }

                    if (obj.Rotation == 3)
                    {
                        _regionService.FlagCollision(x0Y0, CollisionFlag.WallAllowRangeWest | CollisionFlag.WallAllowRangeSouth);
                        _regionService.FlagCollision(x0Ym1, CollisionFlag.WallAllowRangeNorth);
                        _regionService.FlagCollision(xm1Y0, CollisionFlag.WallAllowRangeEast);
                    }
                }
            }
        }

        public void UnFlagWallObject(IGameObject obj)
        {
            var x = obj.Location.X;
            var y = obj.Location.Y;
            var z = obj.Location.Z;

            var xm1Ym1 = Location.Create(x - 1, y - 1, z, obj.Location.Dimension);
            var xm1Y0 = Location.Create(x - 1, y, z, obj.Location.Dimension);
            var xm1Y1 = Location.Create(x - 1, y + 1, z, obj.Location.Dimension);
            var x0Ym1 = Location.Create(x, y - 1, z, obj.Location.Dimension);
            var x0Y0 = Location.Create(x, y, z, obj.Location.Dimension);
            var x0Y1 = Location.Create(x, y + 1, z, obj.Location.Dimension);
            var x1Ym1 = Location.Create(x + 1, y - 1, z, obj.Location.Dimension);
            var x1Y0 = Location.Create(x + 1, y, z, obj.Location.Dimension);
            var x1Y1 = Location.Create(x + 1, y + 1, z, obj.Location.Dimension);

            if (obj.ShapeType == ShapeType.Wall)
            {
                if (obj.Rotation == 0)
                {
                    _regionService.UnFlagCollision(x0Y0, CollisionFlag.WallWest);
                    _regionService.UnFlagCollision(xm1Y0, CollisionFlag.WallEast);
                }

                if (obj.Rotation == 1)
                {
                    _regionService.UnFlagCollision(x0Y0, CollisionFlag.WallNorth);
                    _regionService.UnFlagCollision(x0Y1, CollisionFlag.WallSouth);
                }

                if (obj.Rotation == 2)
                {
                    _regionService.UnFlagCollision(x0Y0, CollisionFlag.WallEast);
                    _regionService.UnFlagCollision(x1Y0, CollisionFlag.WallWest);
                }

                if (obj.Rotation == 3)
                {
                    _regionService.UnFlagCollision(x0Y0, CollisionFlag.WallSouth);
                    _regionService.UnFlagCollision(x0Ym1, CollisionFlag.WallNorth);
                }
            }

            if (obj.ShapeType == ShapeType.WallCornerDiagonal || obj.ShapeType == ShapeType.WallCorner)
            {
                if (obj.Rotation == 0)
                {
                    _regionService.UnFlagCollision(x0Y0, CollisionFlag.WallNorthWest);
                    _regionService.UnFlagCollision(xm1Y1, CollisionFlag.WallSouthEast);
                }

                if (obj.Rotation == 1)
                {
                    _regionService.UnFlagCollision(x0Y0, CollisionFlag.WallNorthEast);
                    _regionService.UnFlagCollision(x1Y1, CollisionFlag.WallSouthWest);
                }

                if (obj.Rotation == 2)
                {
                    _regionService.UnFlagCollision(x0Y0, CollisionFlag.WallSouthEast);
                    _regionService.UnFlagCollision(x1Ym1, CollisionFlag.WallNorthWest);
                }

                if (obj.Rotation == 3)
                {
                    _regionService.UnFlagCollision(x0Y0, CollisionFlag.WallSouthWest);
                    _regionService.UnFlagCollision(xm1Ym1, CollisionFlag.WallNorthEast);
                }
            }

            if (obj.ShapeType == ShapeType.UnfinishedWall)
            {
                if (obj.Rotation == 0)
                {
                    _regionService.UnFlagCollision(x0Y0, CollisionFlag.WallWest | CollisionFlag.WallNorth);
                    _regionService.UnFlagCollision(xm1Y0, CollisionFlag.WallEast);
                    _regionService.UnFlagCollision(x0Y1, CollisionFlag.WallSouth);
                }

                if (obj.Rotation == 1)
                {
                    _regionService.UnFlagCollision(x0Y0, CollisionFlag.WallNorth | CollisionFlag.WallEast);
                    _regionService.UnFlagCollision(x0Y1, CollisionFlag.WallSouth);
                    _regionService.UnFlagCollision(x1Y0, CollisionFlag.WallWest);
                }

                if (obj.Rotation == 2)
                {
                    _regionService.UnFlagCollision(x0Y0, CollisionFlag.WallSouth | CollisionFlag.WallEast);
                    _regionService.UnFlagCollision(x1Y0, CollisionFlag.WallWest);
                    _regionService.UnFlagCollision(x0Ym1, CollisionFlag.WallNorth);
                }

                if (obj.Rotation == 3)
                {
                    _regionService.UnFlagCollision(x0Y0, CollisionFlag.WallWest | CollisionFlag.WallSouth);
                    _regionService.UnFlagCollision(x0Ym1, CollisionFlag.WallNorth);
                    _regionService.UnFlagCollision(xm1Y0, CollisionFlag.WallEast);
                }
            }

            if (obj.Definition.Solid)
            {
                if (obj.ShapeType == ShapeType.Wall)
                {
                    if (obj.Rotation == 0)
                    {
                        _regionService.UnFlagCollision(x0Y0, CollisionFlag.BlockedWest);
                        _regionService.UnFlagCollision(xm1Y0, CollisionFlag.BlockedEast);
                    }

                    if (obj.Rotation == 1)
                    {
                        _regionService.UnFlagCollision(x0Y0, CollisionFlag.BlockedNorth);
                        _regionService.UnFlagCollision(x0Y1, CollisionFlag.BlockedSouth);
                    }

                    if (obj.Rotation == 2)
                    {
                        _regionService.UnFlagCollision(x0Y0, CollisionFlag.BlockedEast);
                        _regionService.UnFlagCollision(x1Y0, CollisionFlag.BlockedWest);
                    }

                    if (obj.Rotation == 3)
                    {
                        _regionService.UnFlagCollision(x0Y0, CollisionFlag.BlockedSouth);
                        _regionService.UnFlagCollision(x0Ym1, CollisionFlag.BlockedNorth);
                    }
                }

                if (obj.ShapeType == ShapeType.WallCornerDiagonal || obj.ShapeType == ShapeType.WallCorner)
                {
                    if (obj.Rotation == 0)
                    {
                        _regionService.UnFlagCollision(x0Y0, CollisionFlag.BlockedNorthWest);
                        _regionService.UnFlagCollision(xm1Y1, CollisionFlag.BlockedSouthWest);
                    }

                    if (obj.Rotation == 1)
                    {
                        _regionService.UnFlagCollision(x0Y0, CollisionFlag.BlockedNorthEast);
                        _regionService.UnFlagCollision(x1Y1, CollisionFlag.BlockedSouthWest);
                    }

                    if (obj.Rotation == 2)
                    {
                        _regionService.UnFlagCollision(x0Y0, CollisionFlag.BlockedSouthWest);
                        _regionService.UnFlagCollision(x1Ym1, CollisionFlag.BlockedNorthWest);
                    }

                    if (obj.Rotation == 3)
                    {
                        _regionService.UnFlagCollision(x0Y0, CollisionFlag.BlockedSouthWest);
                        _regionService.UnFlagCollision(xm1Ym1, CollisionFlag.BlockedNorthEast);
                    }
                }

                if (obj.ShapeType == ShapeType.UnfinishedWall)
                {
                    if (obj.Rotation == 0)
                    {
                        _regionService.UnFlagCollision(x0Y0, CollisionFlag.BlockedWest | CollisionFlag.BlockedNorth);
                        _regionService.UnFlagCollision(xm1Y0, CollisionFlag.BlockedEast);
                        _regionService.UnFlagCollision(x0Y1, CollisionFlag.BlockedSouth);
                    }

                    if (obj.Rotation == 1)
                    {
                        _regionService.UnFlagCollision(x0Y0, CollisionFlag.BlockedEast | CollisionFlag.BlockedNorth);
                        _regionService.UnFlagCollision(x0Y1, CollisionFlag.BlockedSouth);
                        _regionService.UnFlagCollision(x1Y0, CollisionFlag.BlockedWest);
                    }

                    if (obj.Rotation == 2)
                    {
                        _regionService.UnFlagCollision(x0Y0, CollisionFlag.BlockedSouth | CollisionFlag.BlockedEast);
                        _regionService.UnFlagCollision(x1Y0, CollisionFlag.BlockedWest);
                        _regionService.UnFlagCollision(x0Ym1, CollisionFlag.BlockedNorth);
                    }

                    if (obj.Rotation == 3)
                    {
                        _regionService.UnFlagCollision(x0Y0, CollisionFlag.BlockedWest | CollisionFlag.BlockedSouth);
                        _regionService.UnFlagCollision(x0Ym1, CollisionFlag.BlockedNorth);
                        _regionService.UnFlagCollision(xm1Y0, CollisionFlag.BlockedEast);
                    }
                }
            }

            if (!obj.Definition.Gateway)
            {
                if (obj.ShapeType == ShapeType.Wall)
                {
                    if (obj.Rotation == 0)
                    {
                        _regionService.UnFlagCollision(x0Y0, CollisionFlag.WallAllowRangeWest);
                        _regionService.UnFlagCollision(xm1Y0, CollisionFlag.WallAllowRangeEast);
                    }

                    if (obj.Rotation == 1)
                    {
                        _regionService.UnFlagCollision(x0Y0, CollisionFlag.WallAllowRangeNorth);
                        _regionService.UnFlagCollision(x0Y1, CollisionFlag.WallAllowRangeSouth);
                    }

                    if (obj.Rotation == 2)
                    {
                        _regionService.UnFlagCollision(x0Y0, CollisionFlag.WallAllowRangeEast);
                        _regionService.UnFlagCollision(x1Y0, CollisionFlag.WallAllowRangeWest);
                    }

                    if (obj.Rotation == 3)
                    {
                        _regionService.UnFlagCollision(x0Y0, CollisionFlag.WallAllowRangeSouth);
                        _regionService.UnFlagCollision(x0Ym1, CollisionFlag.WallAllowRangeNorth);
                    }
                }

                if (obj.ShapeType == ShapeType.WallCornerDiagonal || obj.ShapeType == ShapeType.WallCorner)
                {
                    if (obj.Rotation == 0)
                    {
                        _regionService.UnFlagCollision(x0Y0, CollisionFlag.WallAllowRangeNorthWest);
                        _regionService.UnFlagCollision(xm1Y1, CollisionFlag.WallAllowRangeSouthEast);
                    }

                    if (obj.Rotation == 1)
                    {
                        _regionService.UnFlagCollision(x0Y0, CollisionFlag.WallAllowRangeNorthEast);
                        _regionService.UnFlagCollision(x1Y1, CollisionFlag.WallAllowRangeSouthWest);
                    }

                    if (obj.Rotation == 2)
                    {
                        _regionService.UnFlagCollision(x0Y0, CollisionFlag.WallAllowRangeSouthEast);
                        _regionService.UnFlagCollision(x1Ym1, CollisionFlag.WallAllowRangeNorthWest);
                    }

                    if (obj.Rotation == 3)
                    {
                        _regionService.UnFlagCollision(x0Y0, CollisionFlag.WallAllowRangeSouthWest);
                        _regionService.UnFlagCollision(xm1Ym1, CollisionFlag.WallAllowRangeNorthEast);
                    }
                }

                if (obj.ShapeType == ShapeType.UnfinishedWall)
                {
                    if (obj.Rotation == 0)
                    {
                        _regionService.UnFlagCollision(x0Y0, CollisionFlag.WallAllowRangeWest | CollisionFlag.WallAllowRangeNorth);
                        _regionService.UnFlagCollision(xm1Y0, CollisionFlag.WallAllowRangeEast);
                        _regionService.UnFlagCollision(x0Y1, CollisionFlag.WallAllowRangeSouth);
                    }

                    if (obj.Rotation == 1)
                    {
                        _regionService.UnFlagCollision(x0Y0, CollisionFlag.WallAllowRangeNorth | CollisionFlag.WallAllowRangeEast);
                        _regionService.UnFlagCollision(x0Y1, CollisionFlag.WallAllowRangeSouth);
                        _regionService.UnFlagCollision(x1Y0, CollisionFlag.WallAllowRangeWest);
                    }

                    if (obj.Rotation == 2)
                    {
                        _regionService.UnFlagCollision(x0Y0, CollisionFlag.WallAllowRangeSouth | CollisionFlag.WallAllowRangeEast);
                        _regionService.UnFlagCollision(x1Y0, CollisionFlag.WallAllowRangeWest);
                        _regionService.UnFlagCollision(x0Ym1, CollisionFlag.WallAllowRangeNorth);
                    }

                    if (obj.Rotation == 3)
                    {
                        _regionService.UnFlagCollision(x0Y0, CollisionFlag.WallAllowRangeWest | CollisionFlag.WallAllowRangeSouth);
                        _regionService.UnFlagCollision(x0Ym1, CollisionFlag.WallAllowRangeNorth);
                        _regionService.UnFlagCollision(xm1Y0, CollisionFlag.WallAllowRangeEast);
                    }
                }
            }
        }

        public void FlagCollision(IGameObject gameObject)
        {
            var layer = gameObject.ShapeType.GetLayerType();
            if (layer == LayerType.Walls)
                FlagWallObject(gameObject);
            else if (layer == LayerType.StandardObjects)
                FlagStandardObject(gameObject);
            else if (layer == LayerType.FloorDecorations)
                FlagFloorDecorationCollision(gameObject);
        }

        public void UnFlagCollision(IGameObject gameObject)
        {
            var layer = gameObject.ShapeType.GetLayerType();
            if (layer == LayerType.Walls)
                UnFlagWallObject(gameObject);
            else if (layer == LayerType.StandardObjects)
                UnFlagStandardObject(gameObject);
            else if (layer == LayerType.FloorDecorations)
                UnFlagFloorDecorationCollision(gameObject);
        }
    }
}