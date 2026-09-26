using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Hagalaz.Services.GameWorld.Metrics;

internal static class GameWorldMetrics
{
    public const string MeterName = "Hagalaz.Services.GameWorld";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> RegionLoads = Meter.CreateCounter<long>(
        "hagalaz.gameworld.map.region.load",
        "{load}");
    private static readonly Histogram<double> RegionLoadDuration = Meter.CreateHistogram<double>(
        "hagalaz.gameworld.map.region.load.duration",
        "s");
    private static readonly Counter<long> DefinitionResolutions = Meter.CreateCounter<long>(
        "hagalaz.gameworld.gameobject.definition.resolve",
        "{batch}");
    private static readonly Histogram<double> DefinitionResolutionDuration = Meter.CreateHistogram<double>(
        "hagalaz.gameworld.gameobject.definition.resolve.duration",
        "s");
    private static readonly Histogram<long> DefinitionResolutionBatchSize = Meter.CreateHistogram<long>(
        "hagalaz.gameworld.gameobject.definition.resolve.batch_size",
        "{definition}");
    private static readonly Counter<long> DefinitionCacheLookups = Meter.CreateCounter<long>(
        "hagalaz.gameworld.gameobject.definition.cache.lookup",
        "{lookup}");

    public static void RecordRegionLoad(string outcome, double durationSeconds)
    {
        var tags = new TagList { { "outcome", outcome } };
        RegionLoads.Add(1, tags);
        RegionLoadDuration.Record(durationSeconds, tags);
    }

    public static void RecordDefinitionResolution(string outcome, int? batchSize, double durationSeconds)
    {
        var tags = new TagList { { "outcome", outcome } };
        DefinitionResolutions.Add(1, tags);
        DefinitionResolutionDuration.Record(durationSeconds, tags);
        if (batchSize is int size)
        {
            DefinitionResolutionBatchSize.Record(size);
        }
    }

    public static void RecordDefinitionCacheLookup(string outcome)
    {
        var tags = new TagList { { "outcome", outcome } };
        DefinitionCacheLookups.Add(1, tags);
    }

}
