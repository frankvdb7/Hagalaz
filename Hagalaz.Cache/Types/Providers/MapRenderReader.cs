using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;

namespace Hagalaz.Cache.Types.Providers;

/// <summary>One request owns the terrain halo and its floor definitions. No persistent render cache.</summary>
public sealed class MapRenderReader(ICacheAPI cache)
{
    public MapRenderRegion Read(int id, int[]? xteaKeys, CancellationToken cancellationToken = default)
    {
        if (id is < 0 or > 65535) throw new ArgumentOutOfRangeException(nameof(id));
        if (xteaKeys is not null && xteaKeys.Length != 4) throw new ArgumentException("Exactly four XTEA keys are required.", nameof(xteaKeys));
        if ((cache.ReadReferenceTable(5).Flags & ReferenceTableFlags.Identifiers) == 0)
            throw new NotSupportedException("The map index has no named reference metadata.");
        var warnings = new List<string>();
        var terrain = ReadTerrain(id, warnings, cancellationToken, out var terrainStatus, out var trailer, readTrailer: true);
        var objects = Array.Empty<MapRenderPlacement>();
        var landscapeStatus = "absent";
        cancellationToken.ThrowIfCancellationRequested();
        var landscapeFile = cache.GetFileId(5, $"l{id >> 8}_{id & 255}");
        if (landscapeFile >= 0)
        {
            try
            {
                using var container = xteaKeys is null ? cache.ReadContainer(5, landscapeFile) : cache.ReadContainer(5, landscapeFile, xteaKeys);
                objects = MapRenderDecoder.DecodeLandscape(container.Data, cancellationToken);
                landscapeStatus = "available";
            }
            catch (Exception error) when (IsDataError(error))
            {
                landscapeStatus = "failed";
                warnings.Add($"Region {id} landscape could not be decoded. Check this region's XTEA keys. {error.Message}");
            }
        }

        var planes = new List<MapRenderPlane>();
        var floors = new List<FloorRenderDefinition>();
        MapRenderPlane? underwaterPlane = null;
        MapRenderPlacement[]? underwaterObjects = null;
        if (terrain is not null)
        {
            var x = id >> 8;
            var y = id & 255;
            var east = x < 255 ? ReadTerrain(id + 256, warnings, cancellationToken, out _, out _) : null;
            var north = y < 255 ? ReadTerrain(id + 1, warnings, cancellationToken, out _, out _) : null;
            var corner = x < 255 && y < 255 ? ReadTerrain(id + 257, warnings, cancellationToken, out _, out _) : null;
            if (east is null || north is null || corner is null) warnings.Add($"Region {id} has an incomplete terrain boundary; tiles with unavailable corner heights cannot be drawn.");
            for (var z = 0; z < 4; z++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var heights = new int?[65 * 65];
                for (var tx = 0; tx <= 64; tx++)
                for (var ty = 0; ty <= 64; ty++)
                {
                    var source = tx < 64 ? ty < 64 ? terrain : north : ty < 64 ? east : corner;
                    heights[tx * 65 + ty] = source?[z].Heights[(tx % 64) * 64 + ty % 64];
                }
                var p = terrain[z];
                planes.Add(new MapRenderPlane(heights, p.Flags, p.Underlays, p.Overlays, p.OverlayShapes, p.OverlayRotations));
            }
            var underwater = ReadUnderwaterTerrain(id, warnings, cancellationToken);
            if (underwater is not null)
            {
                underwaterPlane = ComposeUnderwaterPlane(id, underwater, planes[0], warnings, cancellationToken);
                underwaterObjects = ReadUnderwaterObjects(id, warnings, cancellationToken);
            }
            var surfaces = underwater is null ? terrain : terrain.Append(underwater);
            ReadFloors(surfaces.SelectMany(p => p.Underlays), false, floors, warnings, cancellationToken);
            ReadFloors(surfaces.SelectMany(p => p.Overlays), true, floors, warnings, cancellationToken);
        }
        return new MapRenderRegion(id, id >> 8, id & 255, terrainStatus, landscapeStatus, planes, objects, floors, warnings, trailer.Atmosphere, underwaterPlane, underwaterObjects, trailer.PointLights);
    }

    /// <summary>Every floor a terrain tile can store; underlay opcodes end at 174 and overlay IDs are one byte.</summary>
    public IReadOnlyList<FloorRenderDefinition> ReadFloorCatalog(CancellationToken cancellationToken = default)
    {
        var floors = new List<FloorRenderDefinition>();
        var table = cache.ReadReferenceTable(2);
        foreach (var (group, overlay, limit) in new[] { (1, false, 174), (4, true, 255) })
        {
            foreach (var (member, entry) in table.GetEntry(group)?.Entries ?? [])
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (member + 1 > limit) break;
                try
                {
                    using var data = cache.Read(2, group, entry.Index);
                    floors.Add(MapRenderDecoder.DecodeFloor(data, member + 1, overlay));
                }
                catch (Exception error) when (IsDataError(error)) { /* An unreadable definition cannot be previewed or offered. */ }
            }
        }
        return floors;
    }

    private MapRenderPlane ComposeUnderwaterPlane(int id, TerrainRenderPlane underwater, MapRenderPlane surface, List<string> warnings, CancellationToken cancellationToken)
    {
        // The client adds depths to surface heights across the whole scene; a neighbour without `um` adds nothing.
        var x = id >> 8;
        var y = id & 255;
        var east = x < 255 ? ReadUnderwaterTerrain(id + 256, warnings, cancellationToken) : null;
        var north = y < 255 ? ReadUnderwaterTerrain(id + 1, warnings, cancellationToken) : null;
        var corner = x < 255 && y < 255 ? ReadUnderwaterTerrain(id + 257, warnings, cancellationToken) : null;
        var heights = new int?[65 * 65];
        for (var tx = 0; tx <= 64; tx++)
        for (var ty = 0; ty <= 64; ty++)
        {
            var source = tx < 64 ? ty < 64 ? underwater : north : ty < 64 ? east : corner;
            heights[tx * 65 + ty] = surface.CornerHeights[tx * 65 + ty] + (source?.Heights[(tx % 64) * 64 + ty % 64] ?? 0);
        }
        return new MapRenderPlane(heights, underwater.Flags, underwater.Underlays, underwater.Overlays, underwater.OverlayShapes, underwater.OverlayRotations);
    }

    private TerrainRenderPlane? ReadUnderwaterTerrain(int id, List<string> warnings, CancellationToken cancellationToken)
    {
        var file = cache.GetFileId(5, $"um{id >> 8}_{id & 255}");
        if (file < 0) return null;
        try
        {
            using var container = cache.ReadContainer(5, file);
            return MapRenderDecoder.DecodeUnderwaterTerrain(container.Data, cancellationToken);
        }
        catch (Exception error) when (IsDataError(error))
        {
            warnings.Add($"Region {id} underwater terrain could not be decoded. {error.Message}");
            return null;
        }
    }

    private MapRenderPlacement[] ReadUnderwaterObjects(int id, List<string> warnings, CancellationToken cancellationToken)
    {
        var file = cache.GetFileId(5, $"ul{id >> 8}_{id & 255}");
        if (file < 0) return [];
        try
        {
            using var container = cache.ReadContainer(5, file);
            return MapRenderDecoder.DecodeLandscape(container.Data, cancellationToken);
        }
        catch (Exception error) when (IsDataError(error))
        {
            warnings.Add($"Region {id} underwater objects could not be decoded. {error.Message}");
            return [];
        }
    }

    private TerrainRenderPlane[]? ReadTerrain(int id, List<string> warnings, CancellationToken cancellationToken, out string status,
        out (MapAtmosphere? Atmosphere, IReadOnlyList<MapPointLight>? PointLights) trailer, bool readTrailer = false)
    {
        trailer = default;
        cancellationToken.ThrowIfCancellationRequested();
        var file = cache.GetFileId(5, $"m{id >> 8}_{id & 255}");
        status = "absent";
        if (file < 0) return null;
        try
        {
            using var container = cache.ReadContainer(5, file);
            var terrain = MapRenderDecoder.DecodeTerrain(container.Data, id, cancellationToken);
            if (readTrailer)
            {
                try { trailer = MapRenderDecoder.DecodeTerrainTrailer(container.Data, cancellationToken); }
                catch (EndOfStreamException) { warnings.Add($"Region {id} lighting data is truncated; using default lighting."); }
            }
            status = "available";
            return terrain;
        }
        catch (Exception error) when (IsDataError(error))
        {
            status = "failed";
            warnings.Add($"Region {id} terrain could not be decoded. {error.Message}");
            return null;
        }
    }

    private void ReadFloors(IEnumerable<int> ids, bool overlay, List<FloorRenderDefinition> floors, List<string> warnings, CancellationToken cancellationToken)
    {
        var group = overlay ? 4 : 1;
        foreach (var id in ids.Where(id => id > 0).Distinct().Order())
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                // Read takes the physical member position; floor IDs are member IDs and may be sparse.
                var member = cache.ReadReferenceTable(2).GetEntry(group, id - 1)
                    ?? throw new FileNotFoundException($"Floor definition {id} is absent from the cache reference table.");
                using var data = cache.Read(2, group, member.Index);
                floors.Add(MapRenderDecoder.DecodeFloor(data, id, overlay));
            }
            catch (Exception error) when (IsDataError(error))
            {
                warnings.Add($"{(overlay ? "Overlay" : "Underlay")} {id} is unavailable. {error.Message}");
            }
        }
    }

    internal static bool IsDataError(Exception error) => error is IOException or NotSupportedException or OverflowException or ICSharpCode.SharpZipLib.SharpZipBaseException;
}
