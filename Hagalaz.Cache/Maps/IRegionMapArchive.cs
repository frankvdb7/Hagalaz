using System;
using System.Collections.Generic;
using System.Threading;
using FluentResults;
using Hagalaz.Cache.Types;

namespace Hagalaz.Cache.Maps;

/// <summary>
/// The region map files in cache index 5: their lookup, reads and saves. A save is built from the stored file, refused when the
/// file is absent or cannot be decoded, verified, skipped when it changes nothing, and written with compare-and-swap, retrying when
/// another write landed first.
/// </summary>
/// <remarks>
/// Every save names the file version its edit was drawn from and is refused when the file has been written since, so a stale
/// whole-landscape save cannot replace a newer one. Compare-and-swap keeps the readability check and the write together.
/// A save is reported successful only after reopening the file returns the written data.
/// </remarks>
public interface IRegionMapArchive
{
    /// <summary>Whether index 5 carries the name hashes that region files are found by.</summary>
    bool HasNamedFiles { get; }

    /// <summary>The file ID of the region's file of this kind, or <c>null</c> when it has none.</summary>
    int? Locate(int region, RegionMapFile kind);

    /// <summary>Every region with a terrain or landscape file, in region-coordinate order, from one reference-table read.</summary>
    Result<IReadOnlyList<RegionMapEntry>> ListRegions(CancellationToken cancellationToken = default);

    /// <summary>The revisions of the given regions, from one reference-table read. Every cache write changes a file's version.</summary>
    IReadOnlyDictionary<int, RegionMapRevision> Revisions(IReadOnlyCollection<int> regions);

    /// <summary>Decodes the region's terrain. A trailer that ends early is reported as truncated, not as an error.</summary>
    Result<RegionTerrain> ReadTerrain(int region, CancellationToken cancellationToken = default);

    /// <summary>Decodes the region's landscape placements, decrypting the file first when keys are given.</summary>
    Result<MapRenderPlacement[]> ReadLandscape(int region, int[]? xteaKeys = null, CancellationToken cancellationToken = default);

    /// <summary>Decodes the region's underwater floor depths.</summary>
    Result<TerrainRenderPlane> ReadUnderwaterTerrain(int region, CancellationToken cancellationToken = default);

    /// <summary>Decodes the region's underwater object placements.</summary>
    Result<MapRenderPlacement[]> ReadUnderwaterLandscape(int region, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes stored terrain tiles, verifying that nothing else in the file moved. Fails with <see cref="RegionMapFileConflictError"/>
    /// unless the file is still at <paramref name="expectedVersion"/>, and with <see cref="RegionMapWriteUnverifiedError"/> when reopening
    /// the written file does not return what was written.
    /// </summary>
    Result ApplyTerrain(int region, IReadOnlyCollection<MapTerrainTileChange> changes, int expectedVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the region's landscape placements; refuses a landscape it cannot read, so unseen placements are never lost.
    /// Conflict and verification failures are reported as for <see cref="ApplyTerrain"/>.
    /// A protected landscape is read and written with its <paramref name="xteaKeys"/>, which are never guessed or defaulted: without them, or with
    /// wrong ones, the stored file does not decode and the result is <see cref="RegionMapFileUnreadableError"/> with nothing written.
    /// </summary>
    /// <exception cref="ArgumentException">The keys are not four values, or are all zero.</exception>
    Result ReplaceLandscape(int region, IReadOnlyCollection<MapRenderPlacement> placements, int expectedVersion, int[]? xteaKeys = null, CancellationToken cancellationToken = default);
}
