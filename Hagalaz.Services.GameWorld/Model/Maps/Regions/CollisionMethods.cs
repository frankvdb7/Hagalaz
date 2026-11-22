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
        /// Flags a tile. While this region is still being prepared privately it is not registered with the service, so
        /// its own tiles are flagged directly; every other tile goes through the service.
        /// </summary>
        private void FlagTile(ILocation location, CollisionFlag flag)
        {
            if (OwnsUnpublishedTile(location))
            {
                FlagCollision(location.RegionLocalX, location.RegionLocalY, location.Z, flag);
                return;
            }

            _regionService.FlagCollision(location, flag);
        }

        private void UnFlagTile(ILocation location, CollisionFlag flag)
        {
            if (OwnsUnpublishedTile(location))
            {
                UnFlagCollision(location.RegionLocalX, location.RegionLocalY, location.Z, flag);
                return;
            }

            _regionService.UnFlagCollision(location, flag);
        }

        private bool OwnsUnpublishedTile(ILocation location) =>
            IsUnpublished && location.RegionId == Id && location.Dimension == BaseLocation.Dimension;

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

            var (sizeX, sizeY) = GetStandardObjectDimensions(obj);

            for (var xcalc = obj.Location.X; xcalc < obj.Location.X + sizeX; xcalc++)
                for (var ycalc = obj.Location.Y; ycalc < obj.Location.Y + sizeY; ycalc++)
                    FlagTile(Location.Create(xcalc, ycalc, obj.Location.Z, obj.Location.Dimension), flag);
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

            var (sizeX, sizeY) = GetStandardObjectDimensions(obj);

            for (int xcalc = obj.Location.X; xcalc < obj.Location.X + sizeX; xcalc++)
                for (int ycalc = obj.Location.Y; ycalc < obj.Location.Y + sizeY; ycalc++)
                    UnFlagTile(Location.Create(xcalc, ycalc, obj.Location.Z, obj.Location.Dimension), flag);
        }

        private (int SizeX, int SizeY) GetStandardObjectDimensions(IGameObject obj)
        {
            if (obj.Rotation == 1 || obj.Rotation == 3)
            {
                return (obj.Definition.SizeY, obj.Definition.SizeX);
            }

            return (obj.Definition.SizeX, obj.Definition.SizeY);
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
                    FlagTile(x0Y0, CollisionFlag.WallWest);
                    FlagTile(xm1Y0, CollisionFlag.WallEast);
                }

                if (obj.Rotation == 1)
                {
                    FlagTile(x0Y0, CollisionFlag.WallNorth);
                    FlagTile(x0Y1, CollisionFlag.WallSouth);
                }

                if (obj.Rotation == 2)
                {
                    FlagTile(x0Y0, CollisionFlag.WallEast);
                    FlagTile(x1Y0, CollisionFlag.WallWest);
                }

                if (obj.Rotation == 3)
                {
                    FlagTile(x0Y0, CollisionFlag.WallSouth);
                    FlagTile(x0Ym1, CollisionFlag.WallNorth);
                }
            }

            if (obj.ShapeType == ShapeType.WallCornerDiagonal || obj.ShapeType == ShapeType.WallCorner)
            {
                if (obj.Rotation == 0)
                {
                    FlagTile(x0Y0, CollisionFlag.WallNorthWest);
                    FlagTile(xm1Y1, CollisionFlag.WallSouthEast);
                }

                if (obj.Rotation == 1)
                {
                    FlagTile(x0Y0, CollisionFlag.WallNorthEast);
                    FlagTile(x1Y1, CollisionFlag.WallSouthWest);
                }

                if (obj.Rotation == 2)
                {
                    FlagTile(x0Y0, CollisionFlag.WallSouthEast);
                    FlagTile(x1Ym1, CollisionFlag.WallNorthWest);
                }

                if (obj.Rotation == 3)
                {
                    FlagTile(x0Y0, CollisionFlag.WallSouthWest);
                    FlagTile(xm1Ym1, CollisionFlag.WallNorthEast);
                }
            }

            if (obj.ShapeType == ShapeType.UnfinishedWall)
            {
                if (obj.Rotation == 0)
                {
                    FlagTile(x0Y0, CollisionFlag.WallWest | CollisionFlag.WallNorth);
                    FlagTile(xm1Y0, CollisionFlag.WallEast);
                    FlagTile(x0Y1, CollisionFlag.WallSouth);
                }

                if (obj.Rotation == 1)
                {
                    FlagTile(x0Y0, CollisionFlag.WallNorth | CollisionFlag.WallEast);
                    FlagTile(x0Y1, CollisionFlag.WallSouth);
                    FlagTile(x1Y0, CollisionFlag.WallWest);
                }

                if (obj.Rotation == 2)
                {
                    FlagTile(x0Y0, CollisionFlag.WallSouth | CollisionFlag.WallEast);
                    FlagTile(x1Y0, CollisionFlag.WallWest);
                    FlagTile(x0Ym1, CollisionFlag.WallNorth);
                }

                if (obj.Rotation == 3)
                {
                    FlagTile(x0Y0, CollisionFlag.WallWest | CollisionFlag.WallSouth);
                    FlagTile(x0Ym1, CollisionFlag.WallNorth);
                    FlagTile(xm1Y0, CollisionFlag.WallEast);
                }
            }

            if (obj.Definition.Solid)
            {
                if (obj.ShapeType == ShapeType.Wall)
                {
                    if (obj.Rotation == 0)
                    {
                        FlagTile(x0Y0, CollisionFlag.BlockedWest);
                        FlagTile(xm1Y0, CollisionFlag.BlockedEast);
                    }

                    if (obj.Rotation == 1)
                    {
                        FlagTile(x0Y0, CollisionFlag.BlockedNorth);
                        FlagTile(x0Y1, CollisionFlag.BlockedSouth);
                    }

                    if (obj.Rotation == 2)
                    {
                        FlagTile(x0Y0, CollisionFlag.BlockedEast);
                        FlagTile(x1Y0, CollisionFlag.BlockedWest);
                    }

                    if (obj.Rotation == 3)
                    {
                        FlagTile(x0Y0, CollisionFlag.BlockedSouth);
                        FlagTile(x0Ym1, CollisionFlag.BlockedNorth);
                    }
                }

                if (obj.ShapeType == ShapeType.WallCornerDiagonal || obj.ShapeType == ShapeType.WallCorner)
                {
                    if (obj.Rotation == 0)
                    {
                        FlagTile(x0Y0, CollisionFlag.BlockedNorthWest);
                        FlagTile(xm1Y1, CollisionFlag.BlockedSouthEast);
                    }

                    if (obj.Rotation == 1)
                    {
                        FlagTile(x0Y0, CollisionFlag.BlockedNorthEast);
                        FlagTile(x1Y1, CollisionFlag.BlockedSouthWest);
                    }

                    if (obj.Rotation == 2)
                    {
                        FlagTile(x0Y0, CollisionFlag.BlockedSouthEast);
                        FlagTile(x1Ym1, CollisionFlag.BlockedNorthWest);
                    }

                    if (obj.Rotation == 3)
                    {
                        FlagTile(x0Y0, CollisionFlag.BlockedSouthWest);
                        FlagTile(xm1Ym1, CollisionFlag.BlockedNorthEast);
                    }
                }

                if (obj.ShapeType == ShapeType.UnfinishedWall)
                {
                    if (obj.Rotation == 0)
                    {
                        FlagTile(x0Y0, CollisionFlag.BlockedWest | CollisionFlag.BlockedNorth);
                        FlagTile(xm1Y0, CollisionFlag.BlockedEast);
                        FlagTile(x0Y1, CollisionFlag.BlockedSouth);
                    }

                    if (obj.Rotation == 1)
                    {
                        FlagTile(x0Y0, CollisionFlag.BlockedEast | CollisionFlag.BlockedNorth);
                        FlagTile(x0Y1, CollisionFlag.BlockedSouth);
                        FlagTile(x1Y0, CollisionFlag.BlockedWest);
                    }

                    if (obj.Rotation == 2)
                    {
                        FlagTile(x0Y0, CollisionFlag.BlockedSouth | CollisionFlag.BlockedEast);
                        FlagTile(x1Y0, CollisionFlag.BlockedWest);
                        FlagTile(x0Ym1, CollisionFlag.BlockedNorth);
                    }

                    if (obj.Rotation == 3)
                    {
                        FlagTile(x0Y0, CollisionFlag.BlockedWest | CollisionFlag.BlockedSouth);
                        FlagTile(x0Ym1, CollisionFlag.BlockedNorth);
                        FlagTile(xm1Y0, CollisionFlag.BlockedEast);
                    }
                }
            }

            if (!obj.Definition.Gateway)
            {
                if (obj.ShapeType == ShapeType.Wall)
                {
                    if (obj.Rotation == 0)
                    {
                        FlagTile(x0Y0, CollisionFlag.WallAllowRangeWest);
                        FlagTile(xm1Y0, CollisionFlag.WallAllowRangeEast);
                    }

                    if (obj.Rotation == 1)
                    {
                        FlagTile(x0Y0, CollisionFlag.WallAllowRangeNorth);
                        FlagTile(x0Y1, CollisionFlag.WallAllowRangeSouth);
                    }

                    if (obj.Rotation == 2)
                    {
                        FlagTile(x0Y0, CollisionFlag.WallAllowRangeEast);
                        FlagTile(x1Y0, CollisionFlag.WallAllowRangeWest);
                    }

                    if (obj.Rotation == 3)
                    {
                        FlagTile(x0Y0, CollisionFlag.WallAllowRangeSouth);
                        FlagTile(x0Ym1, CollisionFlag.WallAllowRangeNorth);
                    }
                }

                if (obj.ShapeType == ShapeType.WallCornerDiagonal || obj.ShapeType == ShapeType.WallCorner)
                {
                    if (obj.Rotation == 0)
                    {
                        FlagTile(x0Y0, CollisionFlag.WallAllowRangeNorthWest);
                        FlagTile(xm1Y1, CollisionFlag.WallAllowRangeSouthEast);
                    }

                    if (obj.Rotation == 1)
                    {
                        FlagTile(x0Y0, CollisionFlag.WallAllowRangeNorthEast);
                        FlagTile(x1Y1, CollisionFlag.WallAllowRangeSouthWest);
                    }

                    if (obj.Rotation == 2)
                    {
                        FlagTile(x0Y0, CollisionFlag.WallAllowRangeSouthEast);
                        FlagTile(x1Ym1, CollisionFlag.WallAllowRangeNorthWest);
                    }

                    if (obj.Rotation == 3)
                    {
                        FlagTile(x0Y0, CollisionFlag.WallAllowRangeSouthWest);
                        FlagTile(xm1Ym1, CollisionFlag.WallAllowRangeNorthEast);
                    }
                }

                if (obj.ShapeType == ShapeType.UnfinishedWall)
                {
                    if (obj.Rotation == 0)
                    {
                        FlagTile(x0Y0, CollisionFlag.WallAllowRangeWest | CollisionFlag.WallAllowRangeNorth);
                        FlagTile(xm1Y0, CollisionFlag.WallAllowRangeEast);
                        FlagTile(x0Y1, CollisionFlag.WallAllowRangeSouth);
                    }

                    if (obj.Rotation == 1)
                    {
                        FlagTile(x0Y0, CollisionFlag.WallAllowRangeNorth | CollisionFlag.WallAllowRangeEast);
                        FlagTile(x0Y1, CollisionFlag.WallAllowRangeSouth);
                        FlagTile(x1Y0, CollisionFlag.WallAllowRangeWest);
                    }

                    if (obj.Rotation == 2)
                    {
                        FlagTile(x0Y0, CollisionFlag.WallAllowRangeSouth | CollisionFlag.WallAllowRangeEast);
                        FlagTile(x1Y0, CollisionFlag.WallAllowRangeWest);
                        FlagTile(x0Ym1, CollisionFlag.WallAllowRangeNorth);
                    }

                    if (obj.Rotation == 3)
                    {
                        FlagTile(x0Y0, CollisionFlag.WallAllowRangeWest | CollisionFlag.WallAllowRangeSouth);
                        FlagTile(x0Ym1, CollisionFlag.WallAllowRangeNorth);
                        FlagTile(xm1Y0, CollisionFlag.WallAllowRangeEast);
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
                    UnFlagTile(x0Y0, CollisionFlag.WallWest);
                    UnFlagTile(xm1Y0, CollisionFlag.WallEast);
                }

                if (obj.Rotation == 1)
                {
                    UnFlagTile(x0Y0, CollisionFlag.WallNorth);
                    UnFlagTile(x0Y1, CollisionFlag.WallSouth);
                }

                if (obj.Rotation == 2)
                {
                    UnFlagTile(x0Y0, CollisionFlag.WallEast);
                    UnFlagTile(x1Y0, CollisionFlag.WallWest);
                }

                if (obj.Rotation == 3)
                {
                    UnFlagTile(x0Y0, CollisionFlag.WallSouth);
                    UnFlagTile(x0Ym1, CollisionFlag.WallNorth);
                }
            }

            if (obj.ShapeType == ShapeType.WallCornerDiagonal || obj.ShapeType == ShapeType.WallCorner)
            {
                if (obj.Rotation == 0)
                {
                    UnFlagTile(x0Y0, CollisionFlag.WallNorthWest);
                    UnFlagTile(xm1Y1, CollisionFlag.WallSouthEast);
                }

                if (obj.Rotation == 1)
                {
                    UnFlagTile(x0Y0, CollisionFlag.WallNorthEast);
                    UnFlagTile(x1Y1, CollisionFlag.WallSouthWest);
                }

                if (obj.Rotation == 2)
                {
                    UnFlagTile(x0Y0, CollisionFlag.WallSouthEast);
                    UnFlagTile(x1Ym1, CollisionFlag.WallNorthWest);
                }

                if (obj.Rotation == 3)
                {
                    UnFlagTile(x0Y0, CollisionFlag.WallSouthWest);
                    UnFlagTile(xm1Ym1, CollisionFlag.WallNorthEast);
                }
            }

            if (obj.ShapeType == ShapeType.UnfinishedWall)
            {
                if (obj.Rotation == 0)
                {
                    UnFlagTile(x0Y0, CollisionFlag.WallWest | CollisionFlag.WallNorth);
                    UnFlagTile(xm1Y0, CollisionFlag.WallEast);
                    UnFlagTile(x0Y1, CollisionFlag.WallSouth);
                }

                if (obj.Rotation == 1)
                {
                    UnFlagTile(x0Y0, CollisionFlag.WallNorth | CollisionFlag.WallEast);
                    UnFlagTile(x0Y1, CollisionFlag.WallSouth);
                    UnFlagTile(x1Y0, CollisionFlag.WallWest);
                }

                if (obj.Rotation == 2)
                {
                    UnFlagTile(x0Y0, CollisionFlag.WallSouth | CollisionFlag.WallEast);
                    UnFlagTile(x1Y0, CollisionFlag.WallWest);
                    UnFlagTile(x0Ym1, CollisionFlag.WallNorth);
                }

                if (obj.Rotation == 3)
                {
                    UnFlagTile(x0Y0, CollisionFlag.WallWest | CollisionFlag.WallSouth);
                    UnFlagTile(x0Ym1, CollisionFlag.WallNorth);
                    UnFlagTile(xm1Y0, CollisionFlag.WallEast);
                }
            }

            if (obj.Definition.Solid)
            {
                if (obj.ShapeType == ShapeType.Wall)
                {
                    if (obj.Rotation == 0)
                    {
                        UnFlagTile(x0Y0, CollisionFlag.BlockedWest);
                        UnFlagTile(xm1Y0, CollisionFlag.BlockedEast);
                    }

                    if (obj.Rotation == 1)
                    {
                        UnFlagTile(x0Y0, CollisionFlag.BlockedNorth);
                        UnFlagTile(x0Y1, CollisionFlag.BlockedSouth);
                    }

                    if (obj.Rotation == 2)
                    {
                        UnFlagTile(x0Y0, CollisionFlag.BlockedEast);
                        UnFlagTile(x1Y0, CollisionFlag.BlockedWest);
                    }

                    if (obj.Rotation == 3)
                    {
                        UnFlagTile(x0Y0, CollisionFlag.BlockedSouth);
                        UnFlagTile(x0Ym1, CollisionFlag.BlockedNorth);
                    }
                }

                if (obj.ShapeType == ShapeType.WallCornerDiagonal || obj.ShapeType == ShapeType.WallCorner)
                {
                    if (obj.Rotation == 0)
                    {
                        UnFlagTile(x0Y0, CollisionFlag.BlockedNorthWest);
                        UnFlagTile(xm1Y1, CollisionFlag.BlockedSouthEast);
                    }

                    if (obj.Rotation == 1)
                    {
                        UnFlagTile(x0Y0, CollisionFlag.BlockedNorthEast);
                        UnFlagTile(x1Y1, CollisionFlag.BlockedSouthWest);
                    }

                    if (obj.Rotation == 2)
                    {
                        UnFlagTile(x0Y0, CollisionFlag.BlockedSouthEast);
                        UnFlagTile(x1Ym1, CollisionFlag.BlockedNorthWest);
                    }

                    if (obj.Rotation == 3)
                    {
                        UnFlagTile(x0Y0, CollisionFlag.BlockedSouthWest);
                        UnFlagTile(xm1Ym1, CollisionFlag.BlockedNorthEast);
                    }
                }

                if (obj.ShapeType == ShapeType.UnfinishedWall)
                {
                    if (obj.Rotation == 0)
                    {
                        UnFlagTile(x0Y0, CollisionFlag.BlockedWest | CollisionFlag.BlockedNorth);
                        UnFlagTile(xm1Y0, CollisionFlag.BlockedEast);
                        UnFlagTile(x0Y1, CollisionFlag.BlockedSouth);
                    }

                    if (obj.Rotation == 1)
                    {
                        UnFlagTile(x0Y0, CollisionFlag.BlockedEast | CollisionFlag.BlockedNorth);
                        UnFlagTile(x0Y1, CollisionFlag.BlockedSouth);
                        UnFlagTile(x1Y0, CollisionFlag.BlockedWest);
                    }

                    if (obj.Rotation == 2)
                    {
                        UnFlagTile(x0Y0, CollisionFlag.BlockedSouth | CollisionFlag.BlockedEast);
                        UnFlagTile(x1Y0, CollisionFlag.BlockedWest);
                        UnFlagTile(x0Ym1, CollisionFlag.BlockedNorth);
                    }

                    if (obj.Rotation == 3)
                    {
                        UnFlagTile(x0Y0, CollisionFlag.BlockedWest | CollisionFlag.BlockedSouth);
                        UnFlagTile(x0Ym1, CollisionFlag.BlockedNorth);
                        UnFlagTile(xm1Y0, CollisionFlag.BlockedEast);
                    }
                }
            }

            if (!obj.Definition.Gateway)
            {
                if (obj.ShapeType == ShapeType.Wall)
                {
                    if (obj.Rotation == 0)
                    {
                        UnFlagTile(x0Y0, CollisionFlag.WallAllowRangeWest);
                        UnFlagTile(xm1Y0, CollisionFlag.WallAllowRangeEast);
                    }

                    if (obj.Rotation == 1)
                    {
                        UnFlagTile(x0Y0, CollisionFlag.WallAllowRangeNorth);
                        UnFlagTile(x0Y1, CollisionFlag.WallAllowRangeSouth);
                    }

                    if (obj.Rotation == 2)
                    {
                        UnFlagTile(x0Y0, CollisionFlag.WallAllowRangeEast);
                        UnFlagTile(x1Y0, CollisionFlag.WallAllowRangeWest);
                    }

                    if (obj.Rotation == 3)
                    {
                        UnFlagTile(x0Y0, CollisionFlag.WallAllowRangeSouth);
                        UnFlagTile(x0Ym1, CollisionFlag.WallAllowRangeNorth);
                    }
                }

                if (obj.ShapeType == ShapeType.WallCornerDiagonal || obj.ShapeType == ShapeType.WallCorner)
                {
                    if (obj.Rotation == 0)
                    {
                        UnFlagTile(x0Y0, CollisionFlag.WallAllowRangeNorthWest);
                        UnFlagTile(xm1Y1, CollisionFlag.WallAllowRangeSouthEast);
                    }

                    if (obj.Rotation == 1)
                    {
                        UnFlagTile(x0Y0, CollisionFlag.WallAllowRangeNorthEast);
                        UnFlagTile(x1Y1, CollisionFlag.WallAllowRangeSouthWest);
                    }

                    if (obj.Rotation == 2)
                    {
                        UnFlagTile(x0Y0, CollisionFlag.WallAllowRangeSouthEast);
                        UnFlagTile(x1Ym1, CollisionFlag.WallAllowRangeNorthWest);
                    }

                    if (obj.Rotation == 3)
                    {
                        UnFlagTile(x0Y0, CollisionFlag.WallAllowRangeSouthWest);
                        UnFlagTile(xm1Ym1, CollisionFlag.WallAllowRangeNorthEast);
                    }
                }

                if (obj.ShapeType == ShapeType.UnfinishedWall)
                {
                    if (obj.Rotation == 0)
                    {
                        UnFlagTile(x0Y0, CollisionFlag.WallAllowRangeWest | CollisionFlag.WallAllowRangeNorth);
                        UnFlagTile(xm1Y0, CollisionFlag.WallAllowRangeEast);
                        UnFlagTile(x0Y1, CollisionFlag.WallAllowRangeSouth);
                    }

                    if (obj.Rotation == 1)
                    {
                        UnFlagTile(x0Y0, CollisionFlag.WallAllowRangeNorth | CollisionFlag.WallAllowRangeEast);
                        UnFlagTile(x0Y1, CollisionFlag.WallAllowRangeSouth);
                        UnFlagTile(x1Y0, CollisionFlag.WallAllowRangeWest);
                    }

                    if (obj.Rotation == 2)
                    {
                        UnFlagTile(x0Y0, CollisionFlag.WallAllowRangeSouth | CollisionFlag.WallAllowRangeEast);
                        UnFlagTile(x1Y0, CollisionFlag.WallAllowRangeWest);
                        UnFlagTile(x0Ym1, CollisionFlag.WallAllowRangeNorth);
                    }

                    if (obj.Rotation == 3)
                    {
                        UnFlagTile(x0Y0, CollisionFlag.WallAllowRangeWest | CollisionFlag.WallAllowRangeSouth);
                        UnFlagTile(x0Ym1, CollisionFlag.WallAllowRangeNorth);
                        UnFlagTile(xm1Y0, CollisionFlag.WallAllowRangeEast);
                    }
                }
            }
        }

        /// <summary>
        /// Gives a game object its collision back; a script that cleared it calls this to restore it.
        /// </summary>
        public void FlagCollision(IGameObject gameObject)
        {
            _unclippedObjects.Remove(gameObject);
            FlagCollisionCore(gameObject);
        }

        private void FlagCollisionCore(IGameObject gameObject)
        {
            if (_objectsWithoutCollision.Contains(gameObject) || _unclippedObjects.Contains(gameObject))
            {
                return;
            }

            var layer = gameObject.ShapeType.GetLayerType();
            if (layer == LayerType.Walls)
                FlagWallObject(gameObject);
            else if (layer == LayerType.StandardObjects)
                FlagStandardObject(gameObject);
            else if (layer == LayerType.FloorDecorations)
                FlagFloorDecorationCollision(gameObject);
        }

        /// <summary>
        /// Clears a game object's collision, as a script does while it is open (a cut web). The object
        /// stays unclipped, through rebuilds too, until <see cref="FlagCollision(IGameObject)"/> restores it.
        /// </summary>
        public void UnFlagCollision(IGameObject gameObject)
        {
            UnFlagCollisionCore(gameObject);
            _unclippedObjects.Add(gameObject);
        }

        private void UnFlagCollisionCore(IGameObject gameObject)
        {
            if (_objectsWithoutCollision.Contains(gameObject) || _unclippedObjects.Contains(gameObject))
            {
                return;
            }

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
