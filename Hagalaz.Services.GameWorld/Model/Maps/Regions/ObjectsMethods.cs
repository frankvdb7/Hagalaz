using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Utilities;
using Hagalaz.Game.Extensions;

namespace Hagalaz.Services.GameWorld.Model.Maps.Regions
{
    /// <summary>
    /// Contains methods for editing map objects.
    /// </summary>
    public partial class MapRegion
    {
        public IEnumerable<IGameObject> FindAllGameObjects() => _parts.SelectMany(part => part.FindAllGameObjects());

        public IEnumerable<IGameObject> FindGameObjects(int localX, int localY, int z)
        {
            var standard = FindStandardGameObject(localX, localY, z);
            if (standard != null)
            {
                yield return standard;
            }

            var nonStandard = FindWallDecorationGameObject(localX, localY, z);
            if (nonStandard != null)
            {
                yield return nonStandard;
            }

            var wall = FindWallGameObject(localX, localY, z);
            if (wall != null)
            {
                yield return wall;
            }

            var decoration = FindFloorDecorationGameObject(localX, localY, z);
            if (decoration != null)
            {
                yield return decoration;
            }
        }

        public IGameObject? FindWallGameObject(int localX, int localY, int z)
        {
            var partHash = LocationHelper.GetRegionPartHash(LocalXToPart(localX), LocalYToPart(localY), z);
            return _parts.TryGetValue(partHash, out var part) ? part.FindGameObject(LayerType.Walls, localX, localY, z) : null;
        }

        private IGameObject? FindWallDecorationGameObject(int localX, int localY, int z)
        {
            var partHash = LocationHelper.GetRegionPartHash(LocalXToPart(localX), LocalYToPart(localY), z);
            return _parts.TryGetValue(partHash, out var part) ? part.FindGameObject(LayerType.WallDecorations, localX, localY, z) : null;
        }

        public IGameObject? FindStandardGameObject(int localX, int localY, int z)
        {
            var partHash = LocationHelper.GetRegionPartHash(LocalXToPart(localX), LocalYToPart(localY), z);
            return _parts.TryGetValue(partHash, out var part) ? part.FindGameObject(LayerType.StandardObjects, localX, localY, z) : null;
        }

        public IGameObject? FindFloorDecorationGameObject(int localX, int localY, int z)
        {
            var partHash = LocationHelper.GetRegionPartHash(LocalXToPart(localX), LocalYToPart(localY), z);
            return _parts.TryGetValue(partHash, out var part) ? part.FindGameObject(LayerType.FloorDecorations, localX, localY, z) : null;
        }

        public void Add(IGameObject gameObject)
        {
            _parts
                .GetOrAdd(gameObject.Location.GetRegionPartHash(), CreateRegionPart)
                .Add(gameObject);
            FlagCollision(gameObject);
        }

        public void Remove(IGameObject gameObject)
        {
            var partHash = gameObject.Location.GetRegionPartHash();
            if (!_parts.TryGetValue(partHash, out var part))
            {
                return;
            }

            part.Remove(gameObject);
            UnFlagCollision(gameObject);
        }

        public void UnloadPartGameObjects(int partX, int partY, int partZ, int partRotation)
        {
            var minLocalX = (partX & 0x7) * 8;
            var maxLocalX = minLocalX + 7;
            var minLocalY = (partY & 0x7) * 8;
            var maxLocalY = minLocalY + 7;

            for (var localX = minLocalX; localX <= maxLocalX; localX++)
            {
                for (var localY = minLocalY; localY <= maxLocalY; localY++)
                {
                    var rotatedLocalX = minLocalX + CalculateRotationDelta(localX & 0x7, localY & 0x7, partRotation, false);
                    var rotatedLocalY = minLocalY + CalculateRotationDelta(localX & 0x7, localY & 0x7, partRotation, true);
                    if (rotatedLocalX < 0 || rotatedLocalX >= 64 || rotatedLocalY < 0 || rotatedLocalY >= 64) continue;
                    foreach (var obj in FindGameObjects(rotatedLocalX, rotatedLocalY, partZ)) Remove(obj);
                    SetCollision(rotatedLocalX, rotatedLocalY, partZ, CollisionFlag.Walkable);
                }
            }

            var min = Location.Create(partX + minLocalX, partY + minLocalY, partZ, BaseLocation.Dimension);
            var max = Location.Create(partX + maxLocalX, partY + maxLocalY, partZ, BaseLocation.Dimension);
            // TODO: Dynamic regions
            // foreach (var obj in _disabledStaticObjects.Where(obj =>
            //              obj.Location.X >= min.X && obj.Location.X <= max.X && obj.Location.Y >= min.Y && obj.Location.Y <= max.Y && obj.Location.Z == partZ))
            // {
            //     _disabledStaticObjects.Remove(obj);
            // }
        }

        /// <summary>
        /// Reload's mapregion part.
        /// TODO - Load the original mapregion with object data, npc data etc. 
        /// The original mapregion can be used to cache objects which will then 
        /// be rotated and loaded accordinally here in this dynamic region.
        /// </summary>
        /// <param name="partX">X of the part.</param>
        /// <param name="partY">Y of the part.</param>
        /// <param name="partZ">Z of the part.</param>
        /// <param name="partRotation">The rotation.</param>
        public void LoadPartObjects(int partX, int partY, int partZ, int partRotation)
        {
            var minX = (partX & 0x7) * 8;
            var maxX = minX + 7;
            var minY = (partY & 0x7) * 8;
            var maxY = minY + 7;

            var data = GetRegionPartData(partX & 0x7, partY & 0x7, partZ);
            if (data.GetHashCode() == 0)
            {
                return;
            }

            var regionID = ((data.DrawRegionPartX / 8) << 8) | (data.DrawRegionPartY / 8);
            var region = _regionService.GetOrCreateMapRegion(regionID, BaseLocation.Dimension, false);
            for (var localX = minX; localX <= maxX; localX++)
            {
                for (var localY = minY; localY <= maxY; localY++)
                {
                    AddPartObjects(region, minX, minY, localX, localY, partZ, partRotation);

                    var rotatedLocalX = minX + CalculateRotationDelta(localX & 0x7, localY & 0x7, partRotation, false);
                    var rotatedLocalY = minY + CalculateRotationDelta(localX & 0x7, localY & 0x7, partRotation, true);
                    if (rotatedLocalX < 0 || rotatedLocalX >= 64 || rotatedLocalY < 0 || rotatedLocalY >= 64)
                    {
                        continue;
                    }

                    SetCollision(rotatedLocalX, rotatedLocalY, partZ, region.GetCollision(localX, localY, partZ));
                }
            }
        }

        public void AddPartObjects(IMapRegion region, int minLocalX, int minLocalY, int localX, int localY, int z, int partRotation)
        {
            foreach (var obj in region.FindGameObjects(localX, localY, z))
            {
                var rotatedLocalX = minLocalX + CalculateObjectRotationDelta(obj, localX & 0x7, localY & 0x7, partRotation, false);
                var rotatedLocalY = minLocalY + CalculateObjectRotationDelta(obj, localX & 0x7, localY & 0x7, partRotation, true);

                if (rotatedLocalX >= 0 && rotatedLocalX < Size.X && rotatedLocalY >= 0 && rotatedLocalY < Size.Y)
                {
                    var location = Location.Create(rotatedLocalX + BaseLocation.RegionX * Size.X,
                        rotatedLocalY + BaseLocation.RegionY * Size.Y,
                        z,
                        BaseLocation.Dimension);
                    var builder = _gameObjectBuilder.Create()
                        .WithId(obj.Id)
                        .WithLocation(location)
                        .WithRotation(obj.Rotation)
                        .WithShape(obj.ShapeType);
                    if (obj.IsStatic)
                    {
                        builder.AsStatic();
                    }

                    var gObject = builder.Build();
                    Add(gObject);
                }
            }
        }

        public static int CalculateRotationDelta(int partLocalX, int partLocalY, int partRotation, bool calculateRotationY)
        {
            partRotation &= 0x3;
            if (0 == partRotation) return calculateRotationY ? partLocalY : partLocalX;
            if (1 == partRotation) return calculateRotationY ? 7 - partLocalX : partLocalY;
            if (partRotation == 2) return calculateRotationY ? 7 - partLocalY : 7 - partLocalX;
            return calculateRotationY ? partLocalX : 7 - partLocalY;
        }

        public static int CalculateObjectRotationDelta(IGameObject obj, int partLocalX, int partLocalY, int partRotation, bool calculateRotationY)
        {
            int sizeX;
            int sizeY;
            if ((obj.Rotation & 0x1) == 1)
            {
                sizeX = obj.SizeY;
                sizeY = obj.SizeX;
            }
            else
            {
                sizeX = obj.SizeX;
                sizeY = obj.SizeY;
            }

            partRotation &= 0x3;
            if (partRotation == 0)
                return calculateRotationY ? partLocalY : partLocalX;
            else if (partRotation == 1)
                return calculateRotationY ? 7 - partLocalX - (sizeX - 1) : partLocalY;
            else if (partRotation == 2)
                return calculateRotationY ? 7 - partLocalY - (sizeY - 1) : 7 - partLocalX - (sizeX - 1);
            else
                return calculateRotationY ? partLocalX : 7 - partLocalY - (sizeY - 1);
        }
    }
}