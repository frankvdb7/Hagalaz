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
        var terrain = ReadTerrain(id, warnings, cancellationToken, out var terrainStatus);
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
        if (terrain is not null)
        {
            var x = id >> 8;
            var y = id & 255;
            var east = x < 255 ? ReadTerrain(id + 256, warnings, cancellationToken, out _) : null;
            var north = y < 255 ? ReadTerrain(id + 1, warnings, cancellationToken, out _) : null;
            var corner = x < 255 && y < 255 ? ReadTerrain(id + 257, warnings, cancellationToken, out _) : null;
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
            ReadFloors(terrain.SelectMany(p => p.Underlays), false, floors, warnings, cancellationToken);
            ReadFloors(terrain.SelectMany(p => p.Overlays), true, floors, warnings, cancellationToken);
        }
        return new MapRenderRegion(id, id >> 8, id & 255, terrainStatus, landscapeStatus, planes, objects, floors, warnings);
    }

    private TerrainRenderPlane[]? ReadTerrain(int id, List<string> warnings, CancellationToken cancellationToken, out string status)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var file = cache.GetFileId(5, $"m{id >> 8}_{id & 255}");
        status = "absent";
        if (file < 0) return null;
        try
        {
            using var container = cache.ReadContainer(5, file);
            var terrain = MapRenderDecoder.DecodeTerrain(container.Data, id, cancellationToken);
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
        foreach (var id in ids.Where(id => id > 0).Distinct().Order())
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var data = cache.Read(2, overlay ? 4 : 1, id - 1);
                floors.Add(MapRenderDecoder.DecodeFloor(data, id, overlay));
            }
            catch (Exception error) when (IsDataError(error))
            {
                warnings.Add($"{(overlay ? "Overlay" : "Underlay")} {id} is unavailable. {error.Message}");
            }
        }
    }

    private static bool IsDataError(Exception error) => error is IOException or NotSupportedException or OverflowException or ICSharpCode.SharpZipLib.SharpZipBaseException;
}
