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
    /// Rebuilds the objects and floor collision of 8x8 parts from their source templates.
    /// </summary>
    /// <remarks>
    /// Each part records what it built, because its objects and floor blocks need not land in the
    /// part itself: on bridge tiles they move one plane down, and large rotated objects can root in
    /// a neighbouring column. A part also records the columns whose plane-1 chunk it read bridge
    /// flags from, and the parts it beat for an object slot, so an edit rebuilds the parts whose
    /// content can change and leaves runtime state in the other parts alone.
    /// </remarks>
    public partial class MapRegion
    {
        private readonly Dictionary<int, PartContent> _partContent = new();
        private readonly Dictionary<IGameObject, int> _objectOwners = new(ReferenceEqualityComparer.Instance);

        /// <summary>
        /// Objects other parts' objects displaced during the current rebuild; their collision is
        /// unflagged and overlapping survivors are re-flagged once, after everything is placed.
        /// </summary>
        private readonly List<IGameObject> _overwrittenObjects = [];

        private static int GetColumnKey(int localPartX, int localPartY) => localPartX * PartsPerRegionAxis + localPartY;

        private static int GetPartKey(int localPartX, int localPartY, int z) => GetColumnKey(localPartX, localPartY) * Planes + z;

        /// <summary>
        /// The order the client loads chunk objects in (<c>RegionManager.loadPartObjects</c>): plane,
        /// then x, then y, all ascending. The server holds one object per slot, so when two parts
        /// claim a slot the later one in this order keeps it, whatever order the edits came in.
        /// </summary>
        private static int GetBuildRank(int partKey) => partKey % Planes * PartsPerRegionAxis * PartsPerRegionAxis + partKey / Planes;

        /// <summary>
        /// Applies descriptor changes to parts, then rebuilds every affected part once.
        /// </summary>
        private void EditParts(IReadOnlyList<(int LocalPartX, int LocalPartY, int Z, Action<IMapRegionPart> Change)> edits, IReadOnlyDictionary<int, MapRegionTemplate> templates)
        {
            ArgumentNullException.ThrowIfNull(templates);
            var edited = edits.Select(edit => GetRegionPartByLocalPart(edit.LocalPartX, edit.LocalPartY, edit.Z)).ToList();
            var previous = edited.Select(part => (part.DrawRegionPartX, part.DrawRegionPartY, part.DrawRegionZ, part.HasDrawSource, part.Rotation)).ToList();
            for (var index = 0; index < edits.Count; index++)
            {
                edits[index].Change(edited[index]);
            }

            var editedKeys = edits.Select(edit => GetPartKey(edit.LocalPartX, edit.LocalPartY, edit.Z)).Distinct().ToList();
            List<int> parts;
            try
            {
                EnsureTemplates(editedKeys, templates);
                parts = GetPartsToRebuild(editedKeys, templates);
                EnsureTemplates(parts, templates);
            }
            catch
            {
                for (var index = edited.Count - 1; index >= 0; index--)
                {
                    (edited[index].DrawRegionPartX, edited[index].DrawRegionPartY, edited[index].DrawRegionZ, edited[index].HasDrawSource, edited[index].Rotation) = previous[index];
                }

                throw;
            }

            RebuildParts(parts, editedKeys, templates, edited.Distinct().ToList());
        }

        /// <summary>
        /// The edited parts; for an edited plane-1 chunk whose bridge flags changed, every part that
        /// read the old flags; and, transitively, every part these beat for an object slot, which may
        /// get the slot back.
        /// A part that lost a slot is rebuilt alone: it loses again to the unchanged winner. A column
        /// never built here (a region loaded as standard) is rebuilt whole, since its content is not
        /// recorded per part.
        /// </summary>
        private List<int> GetPartsToRebuild(IReadOnlyCollection<int> editedParts, IReadOnlyDictionary<int, MapRegionTemplate> templates)
        {
            var parts = new HashSet<int>(editedParts);
            var bridgeMasks = editedParts
                .Where(key => key % Planes == 1)
                .Select(key => key / Planes)
                .Distinct()
                .ToDictionary(column => column, column => GetBridgeMask(column, templates));
            parts.UnionWith(_partContent
                .Where(entry => entry.Value.BridgeReads.Any(read => bridgeMasks.TryGetValue(read.Key, out var mask) && mask != read.Value))
                .Select(entry => entry.Key));

            var pending = new Queue<int>(parts);
            while (pending.TryDequeue(out var key))
            {
                var column = key / Planes;
                var untrackedColumn = Enumerable.Range(0, Planes).All(z => !_partContent.ContainsKey(column * Planes + z));
                var related = untrackedColumn
                    ? Enumerable.Range(0, Planes).Select(z => column * Planes + z)
                    : _partContent.TryGetValue(key, out var content) ? content.BeatenParts : [];
                foreach (var part in related.Where(parts.Add))
                {
                    pending.Enqueue(part);
                }
            }

            return parts.OrderBy(GetBuildRank).ToList();
        }

        private void EnsureTemplates(IReadOnlyCollection<int> parts, IReadOnlyDictionary<int, MapRegionTemplate> templates)
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

            foreach (var key in parts)
            {
                var column = key / Planes;
                Require(column / PartsPerRegionAxis, column % PartsPerRegionAxis, key % Planes);
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

        /// <param name="parts">The parts to rebuild.</param>
        /// <param name="editedParts">The parts whose descriptor changed; other parts keep their runtime removals.</param>
        /// <param name="templates">Templates by region id.</param>
        /// <param name="replacedParts">Parts whose chunk was replaced: objects added to them at runtime are removed too.</param>
        private void RebuildParts(IReadOnlyCollection<int> parts, IReadOnlyCollection<int> editedParts, IReadOnlyDictionary<int, MapRegionTemplate> templates, IReadOnlyCollection<IMapRegionPart> replacedParts)
        {
            // A part rebuilt only because something it depends on changed has the same descriptor,
            // so objects removed at runtime stay removed.
            var carried = parts
                .Where(key => !editedParts.Contains(key))
                .Select(key => (Key: key, Tombstones: CollectTombstones(key)))
                .Where(entry => entry.Tombstones.Count > 0)
                .ToDictionary(entry => entry.Key, entry => entry.Tombstones);
            var removed = DiscardRuntimeObjects(replacedParts);
            foreach (var key in parts)
            {
                removed.AddRange(DiscardPart(key));
            }

            foreach (var key in parts.OrderBy(GetBuildRank))
            {
                MaterializePart(key, templates, carried.GetValueOrDefault(key));
            }

            removed.AddRange(_overwrittenObjects);
            _overwrittenObjects.Clear();

            foreach (var plane in removed.GroupBy(obj => obj.Location.Z))
            {
                ReflagOverlappingObjects(plane.ToList(), plane.Key);
            }
        }

        /// <summary>
        /// The slots of a part's objects that were removed at runtime (disabled statics), plus the
        /// removals it already carries from being beaten for a slot.
        /// </summary>
        private HashSet<(int X, int Y, int Z, LayerType Layer, int Id)> CollectTombstones(int key)
        {
            var tombstones = new HashSet<(int X, int Y, int Z, LayerType Layer, int Id)>();
            if (!_partContent.TryGetValue(key, out var content))
            {
                return tombstones;
            }

            tombstones.UnionWith(content.Tombstones);
            foreach (var gameObject in content.Objects.Where(IsDisabledStatic))
            {
                tombstones.Add(GetTombstone(gameObject));
            }

            return tombstones;
        }

        private bool IsDisabledStatic(IGameObject gameObject) =>
            _parts[gameObject.Location.GetRegionPartHash()] is { } part
            && part.FindAllDisabledStaticGameObjects().Any(disabled => ReferenceEquals(disabled, gameObject));

        private static (int X, int Y, int Z, LayerType Layer, int Id) GetTombstone(IGameObject gameObject) =>
            (gameObject.Location.RegionLocalX, gameObject.Location.RegionLocalY, gameObject.Location.Z, gameObject.ShapeType.GetLayerType(), gameObject.Id);

        /// <summary>
        /// Removes objects stored in replaced parts that no part built (added at runtime,
        /// enabled or disabled), leaving objects other parts placed there.
        /// </summary>
        /// <returns>The removed objects whose collision was unflagged.</returns>
        private List<IGameObject> DiscardRuntimeObjects(IReadOnlyCollection<IMapRegionPart> replacedParts)
        {
            var removed = new List<IGameObject>();
            foreach (var part in replacedParts)
            {
                var runtimeObjects = part.FindAllGameObjects().Concat(part.FindAllDisabledStaticGameObjects())
                    .Where(obj => !_objectOwners.ContainsKey(obj))
                    .ToList();
                removed.AddRange(DiscardObjects(part, runtimeObjects));
            }

            return removed;
        }

        /// <summary>
        /// Unflags the enabled objects, then removes and destroys the objects from the part storing them.
        /// </summary>
        private List<IGameObject> DiscardObjects(IMapRegionPart part, IReadOnlyCollection<IGameObject> objects)
        {
            var enabled = objects.Where(OwnsGameObject).ToList();
            foreach (var gameObject in enabled)
            {
                UnFlagCollisionCore(gameObject);
            }

            part.DiscardObjects(objects);
            _objectsWithoutCollision.ExceptWith(objects);
            foreach (var gameObject in objects)
            {
                _objectOwners.Remove(gameObject);
            }

            return enabled;
        }

        /// <summary>
        /// Removes what a part built: its objects (including ones disabled since) and floor blocks,
        /// without recording runtime removals. A part never built here (a region loaded as standard)
        /// gives up everything stored in it that no other part built, and every floor block on its plane.
        /// </summary>
        /// <returns>The removed objects whose collision was unflagged.</returns>
        private List<IGameObject> DiscardPart(int key)
        {
            var column = key / Planes;
            var localPartX = column / PartsPerRegionAxis;
            var localPartY = column % PartsPerRegionAxis;
            var z = key % Planes;
            List<IGameObject> objects;
            IEnumerable<(int X, int Y, int Z)> floorTiles;
            if (_partContent.Remove(key, out var content))
            {
                objects = content.Objects;
                floorTiles = content.FloorTiles;
            }
            else
            {
                objects = [];
                if (_parts.TryGetValue(LocationHelper.GetRegionPartHash(LocalPartXToPartX(localPartX), LocalPartYToPartY(localPartY), z), out var stored))
                {
                    objects.AddRange(stored.FindAllGameObjects().Concat(stored.FindAllDisabledStaticGameObjects())
                        .Where(obj => !_objectOwners.ContainsKey(obj)));
                }

                floorTiles = from x in Enumerable.Range(localPartX * 8, 8)
                    from y in Enumerable.Range(localPartY * 8, 8)
                    select (x, y, z);
            }

            var enabled = new List<IGameObject>();
            var storedByPart = objects
                .GroupBy(obj => obj.Location.GetRegionPartHash())
                .Select(group => (Part: _parts[group.Key], Objects: group.ToList()))
                .Where(group => group.Part is not null);
            foreach (var (part, group) in storedByPart)
            {
                enabled.AddRange(DiscardObjects(part!, group));
            }

            foreach (var (x, y, plane) in floorTiles)
            {
                UnFlagCollision(x, y, plane, CollisionFlag.FloorBlock);
            }

            return enabled;
        }

        /// <summary>
        /// Builds a part as the client draws it: the chunk reads its source 8x8 on its source plane,
        /// rotated; static objects and floor blocks land one plane lower where the destination tile
        /// is a bridge, and objects that would fall below plane 0 stay on their draw plane without
        /// collision.
        /// </summary>
        private void MaterializePart(int key, IReadOnlyDictionary<int, MapRegionTemplate> templates, IReadOnlySet<(int X, int Y, int Z, LayerType Layer, int Id)>? tombstones)
        {
            var content = new PartContent();
            _partContent[key] = content;
            var column = key / Planes;
            var localPartX = column / PartsPerRegionAxis;
            var localPartY = column % PartsPerRegionAxis;
            var z = key % Planes;
            if (!TryGetPartSource(localPartX, localPartY, z, out var source))
            {
                return;
            }

            var minLocalX = localPartX * 8;
            var minLocalY = localPartY * 8;
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
                    var plane = MapTerrain.GetCollisionPlane(z, IsBridgeAt(localX, localY, templates, content));
                    if (plane < 0)
                    {
                        continue;
                    }

                    FlagCollision(localX, localY, plane, CollisionFlag.FloorBlock);
                    content.FloorTiles.Add((localX, localY, plane));
                }
            }

            foreach (var entry in template.GetSourceObjects(source.Z, sourceMinX >> 3, sourceMinY >> 3))
            {
                var sizeX = entry.Definition.SizeX;
                var sizeY = entry.Definition.SizeY;
                var localX = minLocalX + CalculateObjectRotationDelta(sizeX, sizeY, entry.Rotation, entry.LocalX & 0x7, entry.LocalY & 0x7, source.Rotation, false);
                var localY = minLocalY + CalculateObjectRotationDelta(sizeX, sizeY, entry.Rotation, entry.LocalX & 0x7, entry.LocalY & 0x7, source.Rotation, true);
                if (localX < 0 || localX >= Size.X || localY < 0 || localY >= Size.Y)
                {
                    continue;
                }

                // Configured spawns are not part of the client's map, so no bridge applies to them.
                // Where the collision plane falls below plane 0 the client still draws the object
                // on its draw plane, only without clipping it.
                var plane = entry.IsStatic ? MapTerrain.GetCollisionPlane(z, IsBridgeAt(localX, localY, templates, content)) : z;
                var hasCollision = plane >= 0;
                if (!hasCollision)
                {
                    plane = z;
                }

                var tombstone = (localX, localY, plane, entry.Shape.GetLayerType(), entry.Id);
                if (tombstones is not null && tombstones.Contains(tombstone))
                {
                    content.Tombstones.Add(tombstone);
                    continue;
                }

                if (!ClaimSlot(key, content, localX, localY, plane, entry.Shape.GetLayerType(), hasCollision))
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
                if (TryAdd(gameObject, hasCollision))
                {
                    content.Objects.Add(gameObject);
                    _objectOwners[gameObject] = key;
                }
            }
        }

        /// <summary>
        /// Decides whether a part may place an object in a slot that another part's object holds,
        /// enabled or disabled at runtime. An object with collision beats one without; otherwise
        /// the higher build rank wins. The winner records the loser, so the loser is rebuilt (and may
        /// take the slot) when the winner is. A beaten object another part built is removed silently,
        /// since the client never drew it. Slots held by runtime objects follow <see cref="TryAdd"/>.
        /// </summary>
        private bool ClaimSlot(int key, PartContent content, int localX, int localY, int plane, LayerType layer, bool hasCollision)
        {
            bool InSlot(IGameObject existing) => existing.ShapeType.GetLayerType() == layer
                && existing.Location.RegionLocalX == localX && existing.Location.RegionLocalY == localY && existing.Location.Z == plane;

            var storage = _parts[LocationHelper.GetRegionPartHash(LocalXToPart(localX), LocalYToPart(localY), plane)];
            var occupant = FindGameObjects(localX, localY, plane).FirstOrDefault(InSlot)
                           ?? storage?.FindAllDisabledStaticGameObjects().FirstOrDefault(InSlot);
            if (occupant is null || !_objectOwners.TryGetValue(occupant, out var ownerKey) || ownerKey == key)
            {
                return occupant is null || hasCollision;
            }

            var occupantHasCollision = !_objectsWithoutCollision.Contains(occupant);
            var wins = hasCollision == occupantHasCollision
                ? GetBuildRank(ownerKey) < GetBuildRank(key)
                : hasCollision;
            if (!wins)
            {
                if (_partContent.TryGetValue(ownerKey, out var owner))
                {
                    owner.BeatenParts.Add(key);
                }

                return false;
            }

            content.BeatenParts.Add(ownerKey);

            // The beaten part keeps its own runtime removal, so it is still removed if the winner goes.
            if (IsDisabledStatic(occupant) && _partContent.TryGetValue(ownerKey, out var beaten))
            {
                beaten.Tombstones.Add(GetTombstone(occupant));
            }

            _overwrittenObjects.AddRange(DiscardObjects(storage!, [occupant]));
            return true;
        }

        /// <summary>
        /// Whether a region-local tile is a bridge: the plane-1 flag of the source tile that the
        /// chunk drawn on plane 1 places there.
        /// </summary>
        private bool IsBridgeAt(int localX, int localY, IReadOnlyDictionary<int, MapRegionTemplate> templates, PartContent reader)
        {
            var column = GetColumnKey(localX >> 3, localY >> 3);
            if (!reader.BridgeReads.ContainsKey(column))
            {
                reader.BridgeReads[column] = GetBridgeMask(column, templates);
            }

            return IsBridgeTile(localX, localY, templates);
        }

        /// <summary>
        /// The bridge flags of a column's 64 tiles, as the chunk drawn on its plane 1 places them.
        /// </summary>
        private ulong GetBridgeMask(int column, IReadOnlyDictionary<int, MapRegionTemplate> templates)
        {
            var mask = 0UL;
            var minLocalX = column / PartsPerRegionAxis * 8;
            var minLocalY = column % PartsPerRegionAxis * 8;
            for (var x = 0; x < 8; x++)
            {
                for (var y = 0; y < 8; y++)
                {
                    if (IsBridgeTile(minLocalX + x, minLocalY + y, templates))
                    {
                        mask |= 1UL << (x * 8 + y);
                    }
                }
            }

            return mask;
        }

        private bool IsBridgeTile(int localX, int localY, IReadOnlyDictionary<int, MapRegionTemplate> templates)
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

        private sealed class PartContent
        {
            public List<IGameObject> Objects { get; } = [];

            public List<(int X, int Y, int Z)> FloorTiles { get; } = [];

            /// <summary>
            /// The bridge flags of each column's plane-1 chunk, as this part read them when built.
            /// </summary>
            public Dictionary<int, ulong> BridgeReads { get; } = [];

            /// <summary>
            /// Objects of this part that stay removed (removed at runtime) when it is rebuilt unchanged.
            /// </summary>
            public HashSet<(int X, int Y, int Z, LayerType Layer, int Id)> Tombstones { get; } = [];

            /// <summary>
            /// Parts whose claim on an object slot this part beat.
            /// </summary>
            public HashSet<int> BeatenParts { get; } = [];
        }
    }
}
