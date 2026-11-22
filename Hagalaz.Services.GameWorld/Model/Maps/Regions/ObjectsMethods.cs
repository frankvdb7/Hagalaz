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

        public void Add(IGameObject gameObject) => Add(gameObject, true);

        public void Add(IGameObject gameObject, bool flagCollision) => TryAdd(gameObject, flagCollision);

        /// <summary>
        /// Adds an object. An object without collision (drawn under a bridge, unreachable) never
        /// displaces an object in its slot and is skipped instead; an object with collision silently
        /// replaces one without, so the reachable object wins regardless of order.
        /// </summary>
        /// <returns>Whether the object was added.</returns>
        private bool TryAdd(IGameObject gameObject, bool flagCollision)
        {
            EnsureAcceptsMutation();
            var part = _parts.GetOrAdd(gameObject.Location.GetRegionPartHash(), CreateRegionPart);
            var occupant = part.FindGameObject(gameObject.ShapeType.GetLayerType(), gameObject.Location.RegionLocalX, gameObject.Location.RegionLocalY, gameObject.Location.Z);
            if (occupant is not null && !ReferenceEquals(occupant, gameObject))
            {
                if (!flagCollision)
                {
                    return false;
                }

                if (_objectsWithoutCollision.Contains(occupant))
                {
                    part.DiscardObjects([occupant]);
                    _objectsWithoutCollision.Remove(occupant);
                }
            }

            part.Add(gameObject);
            if (flagCollision)
            {
                FlagCollision(gameObject);
            }
            else
            {
                _objectsWithoutCollision.Add(gameObject);
            }

            return true;
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
        /// Unflagging clears shared collision bits, so re-flag every surviving object
        /// whose collision area overlaps the area the removed objects flagged. Removed
        /// footprints can reach into adjacent regions, and survivors rooted there can
        /// overlap them, so resident regions around the affected area are included.
        /// </summary>
        private void ReflagOverlappingObjects(IReadOnlyCollection<IGameObject> removed, int z)
        {
            if (removed.Count == 0)
            {
                return;
            }

            var affected = removed.Select(GetCollisionBounds).Aggregate((a, b) =>
                (Math.Min(a.MinX, b.MinX), Math.Min(a.MinY, b.MinY), Math.Max(a.MaxX, b.MaxX), Math.Max(a.MaxY, b.MaxY)));
            foreach (var region in FindRegionsThatCanOverlap(affected))
            {
                foreach (var survivor in region.FindAllGameObjects().Where(obj => obj.Location.Z == z).ToList())
                {
                    var bounds = GetCollisionBounds(survivor);
                    if (bounds.MinX <= affected.MaxX && bounds.MaxX >= affected.MinX && bounds.MinY <= affected.MaxY && bounds.MaxY >= affected.MinY)
                    {
                        region.FlagCollision(survivor);
                    }
                }
            }
        }

        /// <summary>
        /// This region plus resident regions whose objects can overlap the area: those
        /// covering it, and those one region west or south, since a survivor can be rooted
        /// up to its size before the area. Lookups never create or resume a region.
        /// </summary>
        private IEnumerable<IMapRegion> FindRegionsThatCanOverlap((int MinX, int MinY, int MaxX, int MaxY) area)
        {
            yield return this;
            for (var regionX = (area.MinX >> 6) - 1; regionX <= area.MaxX >> 6; regionX++)
            {
                for (var regionY = (area.MinY >> 6) - 1; regionY <= area.MaxY >> 6; regionY++)
                {
                    var regionId = (regionX << 8) | regionY;
                    if (regionX < 0 || regionY < 0 || regionId == Id)
                    {
                        continue;
                    }

                    var region = _regionService.FindMapRegion(regionId, BaseLocation.Dimension);
                    if (region is not null && region.State != MapRegionState.Discarded)
                    {
                        yield return region;
                    }
                }
            }
        }

        /// <summary>
        /// Absolute tiles an object's collision can touch: its footprint plus the
        /// one-tile ring wall flagging writes to.
        /// </summary>
        private static (int MinX, int MinY, int MaxX, int MaxY) GetCollisionBounds(IGameObject gameObject)
        {
            var (sizeX, sizeY) = (gameObject.Rotation & 0x1) == 1
                ? (gameObject.Definition.SizeY, gameObject.Definition.SizeX)
                : (gameObject.Definition.SizeX, gameObject.Definition.SizeY);
            return (gameObject.Location.X - 1, gameObject.Location.Y - 1, gameObject.Location.X + sizeX, gameObject.Location.Y + sizeY);
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
