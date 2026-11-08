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

public enum MapArchiveUpdateResult
{
    Updated,
    Absent,
    Unreadable,
}

/// <summary>Replaces a region's landscape placements, refusing a landscape it cannot read so that unseen placements are never lost.</summary>
public sealed class MapLandscapeWriter(ICacheAPI cache)
{
    private const int MapsIndex = 5;

    public MapArchiveUpdateResult Replace(int regionId, IReadOnlyCollection<MapRenderPlacement> placements, CancellationToken cancellationToken = default)
    {
        var file = cache.GetFileId(MapsIndex, $"l{regionId >> 8}_{regionId & 255}");
        if (file < 0) return MapArchiveUpdateResult.Absent;

        CompressionType compression;
        try
        {
            using var current = cache.ReadContainer(MapsIndex, file);
            MapRenderDecoder.DecodeLandscape(current.Data, cancellationToken);
            compression = current.CompressionType;
        }
        catch (Exception error) when (error is InvalidDataException || MapRenderReader.IsDataError(error))
        {
            return MapArchiveUpdateResult.Unreadable;
        }

        using var encoded = MapLandscapeEncoder.Encode(placements);
        if (!MapRenderDecoder.DecodeLandscape(encoded, cancellationToken).SequenceEqual(MapLandscapeEncoder.Canonicalize(placements)))
            throw new InvalidOperationException($"Region {regionId} landscape did not survive encoding.");
        encoded.Position = 0;

        cancellationToken.ThrowIfCancellationRequested();
        using var container = new Container(compression, encoded);
        cache.Write(MapsIndex, file, container);
        return MapArchiveUpdateResult.Updated;
    }
}
