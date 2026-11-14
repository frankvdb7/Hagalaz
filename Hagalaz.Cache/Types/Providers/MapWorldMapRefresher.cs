using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Maps;
using Hagalaz.Cache.Models;

namespace Hagalaz.Cache.Types.Providers;

public sealed record MapWorldMapSkip(int Id, string Reason);
public sealed record MapWorldMapRefreshResult(IReadOnlyList<int> Refreshed, IReadOnlyList<int> Unchanged, IReadOnlyList<MapWorldMapSkip> Skipped);

/// <summary>Regenerates regions of the `main` world map from their stored map files, leaving every other block as stored.</summary>
public sealed class MapWorldMapRefresher(ICacheAPI cache, IRegionMapArchive maps, Func<int, ObjectType?> definition)
{
    private const int WorldMapIndex = 23;
    private const string MainArea = "main";
    private const string NotOnMap = "Region is not on the main world map.";

    public static MapWorldMapRefresher Create(ICacheAPI cache, IRegionMapArchive maps, ITypeProvider<IObjectType> objects)
    {
        var known = new Dictionary<int, ObjectType?>();
        return new MapWorldMapRefresher(cache, maps, id =>
        {
            if (!known.TryGetValue(id, out var type)) known[id] = type = TryGet(objects, id);
            return type;
        });
    }

    // An object ID without a stored definition decodes as a default object; only a stored definition that cannot be decoded is left off the map.
    private static ObjectType? TryGet(ITypeProvider<IObjectType> objects, int id)
    {
        try { return objects.Get(id) as ObjectType; }
        catch (Exception error) when (RegionMapArchive.IsCorruptData(error)) { return null; }
    }

    public MapWorldMapRefreshResult Refresh(IReadOnlyCollection<int> regionIds, CancellationToken cancellationToken = default)
    {
        var ids = regionIds.Distinct().ToArray();
        var file = cache.GetFileId(WorldMapIndex, MainArea);
        if (file < 0) return new([], [], ids.Select(id => new MapWorldMapSkip(id, NotOnMap)).ToArray());

        var parts = ReadMainParts();
        var generated = new Dictionary<int, WorldMapTile[]?>();
        var sources = new Dictionary<int, RegionMapRevision>();
        // Built without holding the cache; `main` is written only if no other refresh changed it meanwhile, and the refresh is
        // rebuilt when a region's files were saved after its tiles were generated, since that save's own refresh may already
        // have been written and would otherwise be overwritten with stale tiles.
        while (true)
        {
            using var current = cache.ReadContainer(WorldMapIndex, file);
            var stored = current.Data.ToArray();
            var area = WorldMapAreaCodec.Read(stored);
            var blocks = area.Blocks.Where(block => block.Kind == 0).ToDictionary(block => (block.RegionX, block.RegionY));
            var located = ids.Select(id => (Id: id, Block: FindBlock(id, parts, blocks))).ToArray();
            var onMap = located.Where(region => region.Block is not null).Select(region => region.Id).ToArray();

            var revisions = maps.Revisions(onMap);
            foreach (var id in onMap)
            {
                var source = revisions[id];
                if (sources.TryGetValue(id, out var used) && used == source) continue;
                cancellationToken.ThrowIfCancellationRequested();
                sources[id] = source;
                generated[id] = Generate(id, cancellationToken);
            }

            var (result, replacements) = Compare(located, generated, stored, area);
            if (replacements.Count > 0)
            {
                using var updated = Patch(current.CompressionType, stored, area, replacements);
                if (!cache.TryReplaceContainer(WorldMapIndex, file, stored, updated)) continue;
            }

            revisions = maps.Revisions(onMap);
            if (onMap.All(id => sources[id] == revisions[id])) return result;
        }
    }

    private static (MapWorldMapRefreshResult Result, Dictionary<WorldMapBlock, WorldMapTile[]> Replacements) Compare(
        (int Id, WorldMapBlock? Block)[] located, Dictionary<int, WorldMapTile[]?> generated, byte[] stored, WorldMapAreaFile area)
    {
        var skipped = new List<MapWorldMapSkip>();
        var unchanged = new List<int>();
        var replacements = new Dictionary<WorldMapBlock, WorldMapTile[]>();
        var refreshed = new List<int>();
        foreach (var (id, found) in located)
        {
            if (found is not { } block) { skipped.Add(new(id, NotOnMap)); continue; }
            if (generated.GetValueOrDefault(id) is not { } tiles) { skipped.Add(new(id, "Region's map files could not be read.")); continue; }
            if (SameTiles(WorldMapAreaCodec.DecodeRegion(stored, area, block), tiles)) { unchanged.Add(id); continue; }
            replacements[block] = tiles;
            refreshed.Add(id);
        }
        return (new(refreshed, unchanged, skipped), replacements);
    }

    private IReadOnlyList<WorldMapAreaPart> ReadMainParts()
    {
        using var details = cache.ReadArchive(WorldMapIndex, cache.GetFileId(WorldMapIndex, "details"));
        for (var entry = 0; entry < details.Entries!.Length; entry++)
        {
            var (map, parts) = WorldMapAreaCodec.ReadDetails(details.GetEntry(entry).ToArray());
            if (map == MainArea) return parts;
        }
        return [];
    }

    // Blocks are stored at display coordinates; each part says where its tiles really are.
    private static WorldMapBlock? FindBlock(int id, IReadOnlyList<WorldMapAreaPart> parts, Dictionary<(int, int), WorldMapBlock> blocks)
    {
        var (x, y) = (id >> 8, id & 255);
        if (parts.FirstOrDefault(candidate => candidate.ContainsReal(x * 64, y * 64)) is not { } part) return null;
        return blocks.GetValueOrDefault((x + (part.DisplayMinX - part.MinX) / 64, y + (part.DisplayMinY - part.MinY) / 64));
    }

    private WorldMapTile[]? Generate(int id, CancellationToken cancellationToken)
    {
        var terrain = maps.ReadTerrain(id, cancellationToken);
        if (terrain.IsFailed) return null;
        // Open-water regions have terrain and no landscape file.
        var landscape = maps.ReadLandscape(id, cancellationToken: cancellationToken);
        if (landscape.IsFailed && !landscape.HasError<RegionMapFileAbsentError>()) return null;
        return WorldMapTileGenerator.Generate(terrain.Value.Planes, landscape.IsSuccess ? landscape.Value : [], definition);
    }

    private static Container Patch(CompressionType compression, byte[] stored, WorldMapAreaFile area, Dictionary<WorldMapBlock, WorldMapTile[]> replacements)
    {
        var patched = WorldMapAreaCodec.ReplaceRegions(stored, area, replacements);
        if (!PatchIsExact(stored, area, patched, replacements)) throw new InvalidOperationException("The world map did not survive patching.");
        return new Container(compression, new MemoryStream(patched));
    }

    private static bool PatchIsExact(byte[] stored, WorldMapAreaFile area, byte[] patched, Dictionary<WorldMapBlock, WorldMapTile[]> replacements)
    {
        var updated = WorldMapAreaCodec.Read(patched);
        if (updated.Blocks.Count != area.Blocks.Count || !stored.AsSpan(0, area.PaletteEnd).SequenceEqual(patched.AsSpan(0, updated.PaletteEnd))) return false;
        for (var index = 0; index < area.Blocks.Count; index++)
        {
            var (before, after) = (area.Blocks[index], updated.Blocks[index]);
            var exact = replacements.TryGetValue(before, out var tiles)
                ? SameTiles(WorldMapAreaCodec.DecodeRegion(patched, updated, after), tiles)
                : stored.AsSpan(before.Start, before.End - before.Start).SequenceEqual(patched.AsSpan(after.Start, after.End - after.Start));
            if (!exact) return false;
        }
        return true;
    }

    private static bool SameTiles(WorldMapTile[] left, WorldMapTile[] right) => left.Zip(right).All(pair => SameTile(pair.First, pair.Second));

    private static bool SameTile(WorldMapTile left, WorldMapTile right)
    {
        var (a, b) = (Significant(left), Significant(right));
        return a.Count == b.Count && a.Zip(b).All(pair => SameLayer(pair.First, pair.Second));
    }

    // An empty tile and one bare layer draw the same.
    private static IReadOnlyList<WorldMapLayer> Significant(WorldMapTile tile) =>
        tile.Layers is [{ Underlay: 0, Overlay: 0, Objects.Count: 0 }] ? [] : tile.Layers;

    private static bool SameLayer(WorldMapLayer left, WorldMapLayer right) =>
        left.Underlay == right.Underlay && left.Overlay == right.Overlay && (left.Overlay == 0 || left.Info == right.Info)
        && left.Objects.Order(ObjectOrder).SequenceEqual(right.Objects.Order(ObjectOrder));

    private static readonly Comparer<WorldMapObject> ObjectOrder = Comparer<WorldMapObject>.Create((left, right) =>
        left.Id != right.Id ? left.Id.CompareTo(right.Id) : left.Data.CompareTo(right.Data));
}
