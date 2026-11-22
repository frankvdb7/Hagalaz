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
            EnsureAcceptsMutation();
            _parts.GetOrAdd(gameObject.Location.GetRegionPartHash(), CreateRegionPart).Add(gameObject);
            FlagCollision(gameObject);
        }

        public void Remove(IGameObject gameObject)
        {
            var partHash = gameObject.Location.GetRegionPartHash();
            if (!_parts.TryGetValue(partHash, out var part))
            {
                return;
            }

            if (!OwnsGameObject(gameObject))
            {
                return;
            }

            UnFlagCollisionCore(gameObject);
            part.Remove(gameObject);
        }

        private bool OwnsGameObject(IGameObject gameObject) =>
            FindGameObjects(gameObject.Location.RegionLocalX, gameObject.Location.RegionLocalY, gameObject.Location.Z)
                .Any(existing => ReferenceEquals(existing, gameObject));

        /// <summary>
        /// Removes a part's objects, disabled static objects and floor blocks
        /// without recording runtime removals.
        /// </summary>
        private void DiscardPartContent(IMapRegionPart part, int localPartX, int localPartY, int z)
        {
            foreach (var gameObject in part.FindAllGameObjects().ToList())
            {
                UnFlagCollisionCore(gameObject);
            }

            part.DiscardContent();
            var minLocalX = localPartX * 8;
            var minLocalY = localPartY * 8;
            for (var localX = minLocalX; localX < minLocalX + 8; localX++)
            {
                for (var localY = minLocalY; localY < minLocalY + 8; localY++)
                {
                    UnFlagCollision(localX, localY, z, CollisionFlag.FloorBlock);
                }
            }
        }

        /// <summary>
        /// Builds a present part's objects and floor blocks from the source
        /// template's 8x8 area on the source plane, rotated by the part rotation.
        /// </summary>
        private void MaterializePart(IMapRegionPart part, int localPartX, int localPartY, int z, MapRegionTemplate sourceTemplate)
        {
            if (!part.HasDrawSource)
            {
                return;
            }

            var sourceMinX = (part.DrawRegionPartX & 0x7) * 8;
            var sourceMinY = (part.DrawRegionPartY & 0x7) * 8;
            var sourceZ = part.DrawRegionZ;
            var rotation = part.Rotation & 0x3;
            var minLocalX = localPartX * 8;
            var minLocalY = localPartY * 8;

            bool IsInSourcePart(int localX, int localY, int plane) =>
                plane == sourceZ
                && localX >= sourceMinX && localX < sourceMinX + 8
                && localY >= sourceMinY && localY < sourceMinY + 8;

            foreach (var tile in sourceTemplate.BlockedTiles)
            {
                if (!IsInSourcePart(tile.LocalX, tile.LocalY, tile.Z))
                {
                    continue;
                }

                FlagCollision(
                    minLocalX + CalculateRotationDelta(tile.LocalX & 0x7, tile.LocalY & 0x7, rotation, false),
                    minLocalY + CalculateRotationDelta(tile.LocalX & 0x7, tile.LocalY & 0x7, rotation, true),
                    z,
                    CollisionFlag.FloorBlock);
            }

            foreach (var entry in sourceTemplate.Objects)
            {
                if (!IsInSourcePart(entry.LocalX, entry.LocalY, entry.Z))
                {
                    continue;
                }

                var sizeX = entry.Definition.SizeX;
                var sizeY = entry.Definition.SizeY;
                var localX = minLocalX + CalculateObjectRotationDelta(sizeX, sizeY, entry.Rotation, entry.LocalX & 0x7, entry.LocalY & 0x7, rotation, false);
                var localY = minLocalY + CalculateObjectRotationDelta(sizeX, sizeY, entry.Rotation, entry.LocalX & 0x7, entry.LocalY & 0x7, rotation, true);
                if (localX < 0 || localX >= Size.X || localY < 0 || localY >= Size.Y)
                {
                    continue;
                }

                var location = Location.Create(
                    BaseLocation.RegionX * Size.X + localX,
                    BaseLocation.RegionY * Size.Y + localY,
                    z,
                    BaseLocation.Dimension);
                var builder = _gameObjectBuilder.Create()
                    .WithId(entry.Id)
                    .WithLocation(location)
                    .WithDefinition(entry.Definition)
                    .WithRotation((entry.Rotation + rotation) & 0x3)
                    .WithShape(entry.Shape);
                if (entry.IsStatic)
                {
                    builder = builder.AsStatic();
                }

                Add(builder.Build());
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

        public static int CalculateObjectRotationDelta(int definitionSizeX, int definitionSizeY, int objectRotation, int partLocalX, int partLocalY, int partRotation, bool calculateRotationY)
        {
            var (sizeX, sizeY) = (objectRotation & 0x1) == 1
                ? (definitionSizeY, definitionSizeX)
                : (definitionSizeX, definitionSizeY);

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
