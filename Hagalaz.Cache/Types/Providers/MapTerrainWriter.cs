using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;

namespace Hagalaz.Cache.Types.Providers;

/// <summary>Changes stored terrain tiles and verifies that nothing else in the archive moved before writing.</summary>
public sealed class MapTerrainWriter(ICacheAPI cache)
{
    private const int MapsIndex = 5;

    public MapArchiveUpdateResult Apply(int regionId, IReadOnlyCollection<MapTerrainTileChange> changes, CancellationToken cancellationToken = default)
    {
        var file = cache.GetFileId(MapsIndex, $"m{regionId >> 8}_{regionId & 255}");
        if (file < 0) return MapArchiveUpdateResult.Absent;

        byte[] stored;
        CompressionType compression;
        TerrainRenderPlane[] before;
        try
        {
            using var current = cache.ReadContainer(MapsIndex, file);
            stored = current.Data.ToArray();
            compression = current.CompressionType;
            before = Decode(stored, regionId, cancellationToken);
        }
        catch (Exception error) when (error is InvalidDataException || MapRenderReader.IsDataError(error))
        {
            return MapArchiveUpdateResult.Unreadable;
        }

        var patched = MapTerrainPatcher.Patch(stored, changes);
        if (patched.AsSpan().SequenceEqual(stored)) return MapArchiveUpdateResult.Updated;
        if (!PatchIsExact(stored, patched, before, Decode(patched, regionId, cancellationToken), changes))
            throw new InvalidOperationException($"Region {regionId} terrain did not survive patching.");

        cancellationToken.ThrowIfCancellationRequested();
        using var data = new MemoryStream(patched);
        using var container = new Container(compression, data);
        cache.Write(MapsIndex, file, container);
        return MapArchiveUpdateResult.Updated;
    }

    private static TerrainRenderPlane[] Decode(byte[] terrain, int regionId, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(terrain);
        return MapRenderDecoder.DecodeTerrain(stream, regionId, cancellationToken);
    }

    private static bool PatchIsExact(byte[] stored, byte[] patched, TerrainRenderPlane[] before, TerrainRenderPlane[] after, IReadOnlyCollection<MapTerrainTileChange> changes)
    {
        if (!stored.AsSpan(MapTerrainPatcher.TerrainEnd(stored)).SequenceEqual(patched.AsSpan(MapTerrainPatcher.TerrainEnd(patched)))) return false;
        var changed = changes.GroupBy(change => (change.Plane, change.X * 64 + change.Y)).ToDictionary(group => group.Key, group => group.Last());
        for (var plane = 0; plane < before.Length; plane++)
        for (var tile = 0; tile < before[plane].Heights.Length; tile++)
        {
            changed.TryGetValue((plane, tile), out var change);
            if (!TileIsExact(before, after, plane, tile, change)) return false;
        }
        return true;
    }

    private static bool TileIsExact(TerrainRenderPlane[] before, TerrainRenderPlane[] after, int plane, int tile, MapTerrainTileChange? change)
    {
        var (old, updated) = (before[plane], after[plane]);
        var overlayShape = change?.Overlay switch { null => old.OverlayShapes[tile], 0 => 0, _ => change.OverlayShape ?? 0 };
        var overlayRotation = change?.Overlay switch { null => old.OverlayRotations[tile], 0 => 0, _ => change.OverlayRotation ?? 0 };
        return RelativeHeight(after, plane, tile) == (change?.Height is { } height ? -(height == 1 ? 0 : height) * 32 : RelativeHeight(before, plane, tile))
            && updated.Flags[tile] == (change?.Flags ?? old.Flags[tile])
            && updated.Underlays[tile] == (change?.Underlay ?? old.Underlays[tile])
            && updated.Overlays[tile] == (change?.Overlay ?? old.Overlays[tile])
            && updated.OverlayShapes[tile] == overlayShape
            && updated.OverlayRotations[tile] == overlayRotation;
    }

    private static int RelativeHeight(TerrainRenderPlane[] planes, int plane, int tile) =>
        planes[plane].Heights[tile] - (plane == 0 ? 0 : planes[plane - 1].Heights[tile]);
}
