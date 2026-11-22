using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Extensions;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Services.GameWorld.Model.Maps.Regions
{
    /// <summary>
    /// Rebuilds the objects and floor collision of 8x8 part columns from their source templates.
    /// </summary>
    /// <remarks>
    /// A column is the four planes of one local part index. It is the unit of rebuilding because
    /// the client takes the bridge flag of every tile from the chunk drawn on plane 1, and objects
    /// and floor blocks on bridge tiles move one plane down into another part of the column.
    /// </remarks>
    public partial class MapRegion
    {
        private readonly Dictionary<int, ColumnContent> _columnContent = new();

        private static int GetColumnKey(int localPartX, int localPartY) => localPartX * PartsPerRegionAxis + localPartY;

        private void EditPart(int localPartX, int localPartY, int z, IReadOnlyDictionary<int, MapRegionTemplate> templates, Action<IMapRegionPart> change)
        {
            ArgumentNullException.ThrowIfNull(templates);
            var part = GetRegionPartByLocalPart(localPartX, localPartY, z);
            var previous = (part.DrawRegionPartX, part.DrawRegionPartY, part.DrawRegionZ, part.HasDrawSource, part.Rotation);
            change(part);
            var columns = GetColumnsToRebuild(localPartX, localPartY, z);
            try
            {
                EnsureTemplates(columns, templates);
            }
            catch
            {
                (part.DrawRegionPartX, part.DrawRegionPartY, part.DrawRegionZ, part.HasDrawSource, part.Rotation) = previous;
                throw;
            }

            RebuildColumns(columns, templates);
        }

        /// <summary>
        /// The edited column, plus, when its plane-1 chunk changes, the columns that placed
        /// objects inside it, because those objects take their bridge flag from it.
        /// </summary>
        private List<int> GetColumnsToRebuild(int localPartX, int localPartY, int z)
        {
            var key = GetColumnKey(localPartX, localPartY);
            var columns = new List<int> { key };
            if (z != 1)
            {
                return columns;
            }

            columns.AddRange(_columnContent
                .Where(entry => entry.Key != key && entry.Value.Objects.Any(obj =>
                    obj.Location.RegionLocalX >> 3 == localPartX && obj.Location.RegionLocalY >> 3 == localPartY))
                .Select(entry => entry.Key));
            return columns;
        }

        private void EnsureTemplates(IReadOnlyCollection<int> columns, IReadOnlyDictionary<int, MapRegionTemplate> templates)
        {
            void Require(int localPartX, int localPartY, int z)
            {
                if (!TryGetPartSource(localPartX, localPartY, z, out var source))
                {
                    return;
                }

                var regionId = GetSourceRegionId(source.PartX, source.PartY);
                if (!templates.TryGetValue(regionId, out var template) || template.RegionId != regionId)
                {
                    throw new ArgumentException($"No template was given for source region {regionId}.", nameof(templates));
                }
            }

            foreach (var key in columns)
            {
                for (var z = 0; z < Planes; z++)
                {
                    Require(key / PartsPerRegionAxis, key % PartsPerRegionAxis, z);
                }
            }

            // Bridge flags of any tile can be read from any plane-1 chunk.
            for (var localPartX = 0; localPartX < PartsPerRegionAxis; localPartX++)
            {
                for (var localPartY = 0; localPartY < PartsPerRegionAxis; localPartY++)
                {
                    Require(localPartX, localPartY, 1);
                }
            }
        }

        private void RebuildColumns(IReadOnlyCollection<int> columns, IReadOnlyDictionary<int, MapRegionTemplate> templates)
        {
            var removed = new List<IGameObject>();
            foreach (var key in columns)
            {
                removed.AddRange(DiscardColumn(key));
            }

            foreach (var key in columns)
            {
                MaterializeColumn(key, templates);
            }

            foreach (var plane in removed.GroupBy(obj => obj.Location.Z))
            {
                ReflagOverlappingObjects(plane.ToList(), plane.Key);
            }
        }

        /// <summary>
        /// Removes what a column built: its objects (including ones disabled since) and floor
        /// blocks, without recording runtime removals. A column never built here (a region loaded
        /// as standard) gives up everything stored in its parts and every floor block it covers.
        /// </summary>
        /// <returns>The removed objects whose collision was unflagged.</returns>
        private List<IGameObject> DiscardColumn(int key)
        {
            var localPartX = key / PartsPerRegionAxis;
            var localPartY = key % PartsPerRegionAxis;
            List<IGameObject> objects;
            IEnumerable<(int X, int Y, int Z)> floorTiles;
            if (_columnContent.Remove(key, out var content))
            {
                objects = content.Objects;
                floorTiles = content.FloorTiles;
            }
            else
            {
                objects = [];
                for (var z = 0; z < Planes; z++)
                {
                    if (_parts.TryGetValue(LocationHelper.GetRegionPartHash(LocalPartXToPartX(localPartX), LocalPartYToPartY(localPartY), z), out var part))
                    {
                        objects.AddRange(part.FindAllGameObjects());
                        objects.AddRange(part.FindAllDisabledStaticGameObjects());
                    }
                }

                floorTiles = from z in Enumerable.Range(0, Planes)
                    from x in Enumerable.Range(localPartX * 8, 8)
                    from y in Enumerable.Range(localPartY * 8, 8)
                    select (x, y, z);
            }

            var enabled = objects.Where(OwnsGameObject).ToList();
            foreach (var gameObject in enabled)
            {
                UnFlagCollisionCore(gameObject);
            }

            foreach (var stored in objects.GroupBy(obj => obj.Location.GetRegionPartHash()))
            {
                if (_parts.TryGetValue(stored.Key, out var part))
                {
                    part.DiscardObjects(stored.ToList());
                }
            }

            foreach (var (x, y, z) in floorTiles)
            {
                UnFlagCollision(x, y, z, CollisionFlag.FloorBlock);
            }

            return enabled;
        }

        /// <summary>
        /// Builds a column as the client draws it: each present chunk reads its source 8x8 on its
        /// source plane, rotated; static objects and floor blocks land one plane lower where the
        /// destination tile is a bridge.
        /// </summary>
        private void MaterializeColumn(int key, IReadOnlyDictionary<int, MapRegionTemplate> templates)
        {
            var content = new ColumnContent();
            _columnContent[key] = content;
            var localPartX = key / PartsPerRegionAxis;
            var localPartY = key % PartsPerRegionAxis;
            var minLocalX = localPartX * 8;
            var minLocalY = localPartY * 8;
            for (var z = 0; z < Planes; z++)
            {
                if (!TryGetPartSource(localPartX, localPartY, z, out var source))
                {
                    continue;
                }

                var template = templates[GetSourceRegionId(source.PartX, source.PartY)];
                var sourceMinX = (source.PartX & 0x7) * 8;
                var sourceMinY = (source.PartY & 0x7) * 8;
                for (var sourceX = 0; sourceX < 8; sourceX++)
                {
                    for (var sourceY = 0; sourceY < 8; sourceY++)
                    {
                        if (!MapTerrain.IsBlocked(template.GetTerrainFlags(source.Z, sourceMinX + sourceX, sourceMinY + sourceY)))
                        {
                            continue;
                        }

                        var localX = minLocalX + CalculateRotationDelta(sourceX, sourceY, source.Rotation, false);
                        var localY = minLocalY + CalculateRotationDelta(sourceX, sourceY, source.Rotation, true);
                        var plane = MapTerrain.GetCollisionPlane(z, IsBridgeAt(localX, localY, templates));
                        if (plane < 0)
                        {
                            continue;
                        }

                        FlagCollision(localX, localY, plane, CollisionFlag.FloorBlock);
                        content.FloorTiles.Add((localX, localY, plane));
                    }
                }

                foreach (var entry in template.SourceObjects)
                {
                    if (entry.Z != source.Z
                        || entry.LocalX < sourceMinX || entry.LocalX >= sourceMinX + 8
                        || entry.LocalY < sourceMinY || entry.LocalY >= sourceMinY + 8)
                    {
                        continue;
                    }

                    var sizeX = entry.Definition.SizeX;
                    var sizeY = entry.Definition.SizeY;
                    var localX = minLocalX + CalculateObjectRotationDelta(sizeX, sizeY, entry.Rotation, entry.LocalX & 0x7, entry.LocalY & 0x7, source.Rotation, false);
                    var localY = minLocalY + CalculateObjectRotationDelta(sizeX, sizeY, entry.Rotation, entry.LocalX & 0x7, entry.LocalY & 0x7, source.Rotation, true);
                    if (localX < 0 || localX >= Size.X || localY < 0 || localY >= Size.Y)
                    {
                        continue;
                    }

                    // Configured spawns are not part of the client's map, so no bridge applies to them.
                    var plane = entry.IsStatic ? MapTerrain.GetCollisionPlane(z, IsBridgeAt(localX, localY, templates)) : z;
                    if (plane < 0)
                    {
                        continue;
                    }

                    var location = Location.Create(
                        BaseLocation.RegionX * Size.X + localX,
                        BaseLocation.RegionY * Size.Y + localY,
                        plane,
                        BaseLocation.Dimension);
                    var builder = _gameObjectBuilder.Create()
                        .WithId(entry.Id)
                        .WithLocation(location)
                        .WithDefinition(entry.Definition)
                        .WithRotation((entry.Rotation + source.Rotation) & 0x3)
                        .WithShape(entry.Shape);
                    if (entry.IsStatic)
                    {
                        builder = builder.AsStatic();
                    }

                    var gameObject = builder.Build();
                    Add(gameObject);
                    content.Objects.Add(gameObject);
                }
            }
        }

        /// <summary>
        /// Whether a region-local tile is a bridge: the plane-1 flag of the source tile that the
        /// chunk drawn on plane 1 places there.
        /// </summary>
        private bool IsBridgeAt(int localX, int localY, IReadOnlyDictionary<int, MapRegionTemplate> templates)
        {
            if (!TryGetPartSource(localX >> 3, localY >> 3, 1, out var source))
            {
                return false;
            }

            var inverseRotation = (4 - source.Rotation) & 0x3;
            var sourceX = (source.PartX & 0x7) * 8 + CalculateRotationDelta(localX & 0x7, localY & 0x7, inverseRotation, false);
            var sourceY = (source.PartY & 0x7) * 8 + CalculateRotationDelta(localX & 0x7, localY & 0x7, inverseRotation, true);
            return MapTerrain.IsBridge(templates[GetSourceRegionId(source.PartX, source.PartY)].GetTerrainFlags(source.Z, sourceX, sourceY));
        }

        /// <summary>
        /// The source a part draws, without creating it. A part that does not exist yet draws
        /// itself in a standard region and nothing in a dynamic one.
        /// </summary>
        private bool TryGetPartSource(int localPartX, int localPartY, int z, out (int PartX, int PartY, int Z, int Rotation) source)
        {
            var partX = LocalPartXToPartX(localPartX);
            var partY = LocalPartYToPartY(localPartY);
            if (_parts.TryGetValue(LocationHelper.GetRegionPartHash(partX, partY, z), out var part))
            {
                source = (part.DrawRegionPartX, part.DrawRegionPartY, part.DrawRegionZ, part.Rotation & 0x3);
                return part.HasDrawSource;
            }

            source = (partX, partY, z, 0);
            return !IsDynamic;
        }

        private sealed class ColumnContent
        {
            public List<IGameObject> Objects { get; } = [];

            public List<(int X, int Y, int Z)> FloorTiles { get; } = [];
        }
    }
}
