using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;

namespace Hagalaz.Cache.Types.Providers;

public sealed record MapWorldMapSkip(int Id, string Reason);
public sealed record MapWorldMapRefreshResult(IReadOnlyList<int> Refreshed, IReadOnlyList<int> Unchanged, IReadOnlyList<MapWorldMapSkip> Skipped);

/// <summary>Regenerates regions of the `main` world map from their stored map files, leaving every other block as stored.</summary>
public sealed class MapWorldMapRefresher(ICacheAPI cache, Func<int, ObjectType?> definition)
{
    private const int MapsIndex = 5;
    private const int WorldMapIndex = 23;
    private const string MainArea = "main";
    private const string NotOnMap = "Region is not on the main world map.";

    public static MapWorldMapRefresher Create(ICacheAPI cache, ITypeProvider<IObjectType> objects)
    {
        var known = new Dictionary<int, ObjectType?>();
        return new MapWorldMapRefresher(cache, id =>
        {
            if (!known.TryGetValue(id, out var type)) known[id] = type = TryGet(objects, id);
            return type;
        });
    }

    private static ObjectType? TryGet(ITypeProvider<IObjectType> objects, int id)
    {
        try { return objects.Get(id) as ObjectType; }
        catch (Exception error) when (IsReadError(error)) { return null; }
    }

    public MapWorldMapRefreshResult Refresh(IReadOnlyCollection<int> regionIds, CancellationToken cancellationToken = default)
    {
        var ids = regionIds.Distinct().ToArray();
        var file = cache.GetFileId(WorldMapIndex, MainArea);
        if (file < 0) return new([], [], ids.Select(id => new MapWorldMapSkip(id, NotOnMap)).ToArray());

        var parts = ReadMainParts();
        var generated = new Dictionary<int, WorldMapTile[]?>();
        var sources = new Dictionary<int, (int Terrain, int Landscape)>();
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

            var table = cache.ReadReferenceTable(MapsIndex);
            foreach (var id in onMap)
            {
                var source = SourceVersions(table, id);
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

            table = cache.ReadReferenceTable(MapsIndex);
            if (onMap.All(id => sources[id] == SourceVersions(table, id))) return result;
        }
    }

    /// <summary>The stored versions of a region's terrain and landscape files; every cache write bumps them.</summary>
    private static (int Terrain, int Landscape) SourceVersions(IReferenceTable table, int id)
    {
        var (x, y) = (id >> 8, id & 255);
        return (Version($"m{x}_{y}"), Version($"l{x}_{y}"));

        int Version(string name) => table.GetFileId(name) is var file and >= 0 ? table.GetEntry(file)?.Version ?? -1 : -1;
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
        var (x, y) = (id >> 8, id & 255);
        var terrainFile = cache.GetFileId(MapsIndex, $"m{x}_{y}");
        if (terrainFile < 0) return null;
        try
        {
            using var terrain = cache.ReadContainer(MapsIndex, terrainFile);
            var planes = MapRenderDecoder.DecodeTerrain(terrain.Data, id, cancellationToken);
            return WorldMapTileGenerator.Generate(planes, ReadPlacements(x, y, cancellationToken), definition);
        }
        catch (Exception error) when (IsReadError(error)) { return null; }
    }

    // Open-water regions have terrain and no landscape file.
    private MapRenderPlacement[] ReadPlacements(int x, int y, CancellationToken cancellationToken)
    {
        var file = cache.GetFileId(MapsIndex, $"l{x}_{y}");
        if (file < 0) return [];
        using var landscape = cache.ReadContainer(MapsIndex, file);
        return MapRenderDecoder.DecodeLandscape(landscape.Data, cancellationToken);
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

    private static bool IsReadError(Exception error) => error is InvalidDataException or ArgumentException || MapRenderReader.IsDataError(error);
}
