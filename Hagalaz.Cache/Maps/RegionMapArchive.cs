using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using FluentResults;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Types;

namespace Hagalaz.Cache.Maps;

/// <summary>A kind of region map file in cache index 5.</summary>
public enum RegionMapFile
{
    /// <summary>Terrain tiles, stored as <c>m{x}_{y}</c>.</summary>
    Terrain,
    /// <summary>Surface object placements, stored as <c>l{x}_{y}</c>.</summary>
    Landscape,
    /// <summary>Underwater floor depths, stored as <c>um{x}_{y}</c>.</summary>
    UnderwaterTerrain,
    /// <summary>Underwater object placements, stored as <c>ul{x}_{y}</c>.</summary>
    UnderwaterLandscape
}

/// <summary>A region that has a terrain or landscape file, with the file IDs it has.</summary>
public sealed record RegionMapEntry(int RegionId, int X, int Y, int? TerrainFile, int? LandscapeFile);

/// <summary>The reference-table versions of a region's terrain and landscape files; -1 for an absent file.</summary>
public readonly record struct RegionMapRevision(int Terrain, int Landscape);

/// <summary>A region's decoded terrain and the atmosphere and lights stored after its last tile.</summary>
public sealed record RegionTerrain(TerrainRenderPlane[] Planes, MapAtmosphere? Atmosphere, IReadOnlyList<MapPointLight>? PointLights, bool TrailerTruncated);

/// <summary>The region has no file of this kind.</summary>
public sealed class RegionMapFileAbsentError(int region, RegionMapFile kind) : Error($"Region {region} has no {RegionMapArchive.Describe(kind)} file.")
{
    public int Region { get; } = region;
    public RegionMapFile Kind { get; } = kind;
}

/// <summary>The region's stored file of this kind cannot be decoded.</summary>
public sealed class RegionMapFileUnreadableError : Error
{
    public RegionMapFileUnreadableError(int region, RegionMapFile kind, Exception cause)
        : base($"Region {region}'s {RegionMapArchive.Describe(kind)} file could not be decoded.")
    {
        Region = region;
        Kind = kind;
        Cause = cause;
        CausedBy(cause);
    }

    public int Region { get; }
    public RegionMapFile Kind { get; }
    public Exception Cause { get; }
}

/// <summary>Index 5 has no named reference metadata, so region files cannot be found by name.</summary>
public sealed class RegionMapNamesMissingError() : Error("The map index has no named reference metadata.");

/// <summary>
/// Owns the region map files in cache index 5: their names and lookup, reading and decoding them, which read failures count as
/// corrupt data, and the write rules, keeping the stored compression on every save.
/// </summary>
/// <remarks>A save whose encoding fails verification is a codec defect and throws <see cref="InvalidOperationException"/> without writing.</remarks>
public sealed class RegionMapArchive(ICacheAPI cache) : IRegionMapArchive
{
    /// <summary>The cache index holding the region map files, for callers that read a located file's raw container.</summary>
    internal const int Index = 5;

    /// <inheritdoc />
    public bool HasNamedFiles => (cache.ReadReferenceTable(Index).Flags & ReferenceTableFlags.Identifiers) != 0;

    /// <inheritdoc />
    public int? Locate(int region, RegionMapFile kind) => cache.GetFileId(Index, Name(region, kind)) is var file and >= 0 ? file : null;

    /// <inheritdoc />
    public Result<IReadOnlyList<RegionMapEntry>> ListRegions(CancellationToken cancellationToken = default)
    {
        var table = cache.ReadReferenceTable(Index);
        if ((table.Flags & ReferenceTableFlags.Identifiers) == 0) return new RegionMapNamesMissingError();

        // Reference-table entry IDs are name hashes, not packed region IDs.
        var files = new Dictionary<int, int>();
        foreach (var (fileId, entry) in table.Entries) files.TryAdd(entry.Id, fileId);
        var regions = new List<RegionMapEntry>();
        for (var x = 0; x < 256; x++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var y = 0; y < 256; y++)
            {
                var region = (x << 8) | y;
                var (terrain, landscape) = (Find(region, RegionMapFile.Terrain), Find(region, RegionMapFile.Landscape));
                if (terrain is not null || landscape is not null) regions.Add(new RegionMapEntry(region, x, y, terrain, landscape));
            }
        }
        return regions;

        int? Find(int region, RegionMapFile kind) => files.TryGetValue(ReferenceTable.CalculateId(Name(region, kind)), out var file) ? file : null;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<int, RegionMapRevision> Revisions(IReadOnlyCollection<int> regions)
    {
        var table = cache.ReadReferenceTable(Index);
        return regions.Distinct().ToDictionary(region => region, region => new RegionMapRevision(Version(region, RegionMapFile.Terrain), Version(region, RegionMapFile.Landscape)));

        int Version(int region, RegionMapFile kind) => table.GetFileId(Name(region, kind)) is var file and >= 0 ? table.GetEntry(file)?.Version ?? -1 : -1;
    }

    /// <inheritdoc />
    public Result<RegionTerrain> ReadTerrain(int region, CancellationToken cancellationToken = default) =>
        Read(region, RegionMapFile.Terrain, null, data =>
        {
            var planes = MapRenderDecoder.DecodeTerrain(data, region, cancellationToken);
            try
            {
                var (atmosphere, pointLights) = MapRenderDecoder.DecodeTerrainTrailer(data, cancellationToken);
                return new RegionTerrain(planes, atmosphere, pointLights, false);
            }
            catch (EndOfStreamException)
            {
                return new RegionTerrain(planes, null, null, true);
            }
        });

    /// <inheritdoc />
    public Result<MapRenderPlacement[]> ReadLandscape(int region, int[]? xteaKeys = null, CancellationToken cancellationToken = default) =>
        Read(region, RegionMapFile.Landscape, xteaKeys, data => MapRenderDecoder.DecodeLandscape(data, cancellationToken));

    /// <inheritdoc />
    public Result<TerrainRenderPlane> ReadUnderwaterTerrain(int region, CancellationToken cancellationToken = default) =>
        Read(region, RegionMapFile.UnderwaterTerrain, null, data => MapRenderDecoder.DecodeUnderwaterTerrain(data, cancellationToken));

    /// <inheritdoc />
    public Result<MapRenderPlacement[]> ReadUnderwaterLandscape(int region, CancellationToken cancellationToken = default) =>
        Read(region, RegionMapFile.UnderwaterLandscape, null, data => MapRenderDecoder.DecodeLandscape(data, cancellationToken));

    /// <inheritdoc />
    public Result ApplyTerrain(int region, IReadOnlyCollection<MapTerrainTileChange> changes, CancellationToken cancellationToken = default) =>
        Rewrite(region, RegionMapFile.Terrain, cancellationToken, stored => DecodeTerrain(stored, region, cancellationToken), (stored, before) =>
        {
            var patched = MapTerrainPatcher.Patch(stored, changes);
            if (patched.AsSpan().SequenceEqual(stored)) return null;
            if (!TerrainPatchIsExact(stored, patched, before, DecodeTerrain(patched, region, cancellationToken), changes))
                throw new InvalidOperationException($"Region {region} terrain did not survive patching.");
            return patched;
        });

    /// <inheritdoc />
    public Result ReplaceLandscape(int region, IReadOnlyCollection<MapRenderPlacement> placements, CancellationToken cancellationToken = default) =>
        Rewrite(region, RegionMapFile.Landscape, cancellationToken, stored => DecodeLandscape(stored, cancellationToken), (_, current) =>
        {
            var encoded = Encode(placements);
            if (!DecodeLandscape(encoded, cancellationToken).SequenceEqual(MapLandscapeEncoder.Canonicalize(placements)))
                throw new InvalidOperationException($"Region {region} landscape did not survive encoding.");
            return encoded.AsSpan().SequenceEqual(Encode(current)) ? null : encoded;
        });

    /// <summary>Whether a read failure means the stored data cannot be decoded, rather than a defect or a cancellation.</summary>
    internal static bool IsCorruptData(Exception error) =>
        error is IOException or InvalidDataException or NotSupportedException or OverflowException or ICSharpCode.SharpZipLib.SharpZipBaseException;

    internal static string Describe(RegionMapFile kind) => kind switch
    {
        RegionMapFile.Terrain => "terrain",
        RegionMapFile.Landscape => "landscape",
        RegionMapFile.UnderwaterTerrain => "underwater terrain",
        RegionMapFile.UnderwaterLandscape => "underwater landscape",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static string Name(int region, RegionMapFile kind)
    {
        var prefix = kind switch
        {
            RegionMapFile.Terrain => "m",
            RegionMapFile.Landscape => "l",
            RegionMapFile.UnderwaterTerrain => "um",
            RegionMapFile.UnderwaterLandscape => "ul",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        return $"{prefix}{region >> 8}_{region & 255}";
    }

    private Result<T> Read<T>(int region, RegionMapFile kind, int[]? xteaKeys, Func<Stream, T> decode)
    {
        if (Locate(region, kind) is not { } file) return new RegionMapFileAbsentError(region, kind);
        try
        {
            using var container = xteaKeys is null ? cache.ReadContainer(Index, file) : cache.ReadContainer(Index, file, xteaKeys);
            return decode(container.Data);
        }
        catch (Exception error) when (IsCorruptData(error))
        {
            return new RegionMapFileUnreadableError(region, kind, error);
        }
    }

    // The single write path: `rebuild` returns the replacement, or null when the stored file would not change.
    private Result Rewrite<TDecoded>(int region, RegionMapFile kind, CancellationToken cancellationToken, Func<byte[], TDecoded> decode, Func<byte[], TDecoded, byte[]?> rebuild)
    {
        if (Locate(region, kind) is not { } file) return new RegionMapFileAbsentError(region, kind);

        // Built without holding the cache, then written only if no other write landed on the file meanwhile.
        while (true)
        {
            byte[] stored;
            CompressionType compression;
            TDecoded current;
            try
            {
                using var container = cache.ReadContainer(Index, file);
                stored = container.Data.ToArray();
                compression = container.CompressionType;
                current = decode(stored);
            }
            catch (Exception error) when (IsCorruptData(error))
            {
                return new RegionMapFileUnreadableError(region, kind, error);
            }

            if (rebuild(stored, current) is not { } replacement) return Result.Ok();

            cancellationToken.ThrowIfCancellationRequested();
            using var updated = new Container(compression, new MemoryStream(replacement));
            if (cache.TryReplaceContainer(Index, file, stored, updated)) return Result.Ok();
        }
    }

    private static TerrainRenderPlane[] DecodeTerrain(byte[] terrain, int region, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(terrain, writable: false);
        return MapRenderDecoder.DecodeTerrain(stream, region, cancellationToken);
    }

    private static MapRenderPlacement[] DecodeLandscape(byte[] landscape, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(landscape, writable: false);
        return MapRenderDecoder.DecodeLandscape(stream, cancellationToken);
    }

    private static byte[] Encode(IEnumerable<MapRenderPlacement> placements)
    {
        using var encoded = MapLandscapeEncoder.Encode(placements);
        return encoded.ToArray();
    }

    private static bool TerrainPatchIsExact(byte[] stored, byte[] patched, TerrainRenderPlane[] before, TerrainRenderPlane[] after, IReadOnlyCollection<MapTerrainTileChange> changes)
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
