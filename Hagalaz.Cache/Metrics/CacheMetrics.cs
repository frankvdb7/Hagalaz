using System.Diagnostics;
using System.Diagnostics.Metrics;
using Hagalaz.Cache.Abstractions.Model;

namespace Hagalaz.Cache.Metrics;

internal static class CacheMetrics
{
    public const string MeterName = "Hagalaz.Cache";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> ArchiveLoads = Meter.CreateCounter<long>(
        "hagalaz.cache.archive.load",
        "{load}");
    private static readonly Histogram<double> ArchiveLoadDuration = Meter.CreateHistogram<double>(
        "hagalaz.cache.archive.load.duration",
        "s");
    private static readonly Histogram<double> ContainerDecodeDuration = Meter.CreateHistogram<double>(
        "hagalaz.cache.container.decode.duration",
        "s");

    public static void RecordArchiveLoad(bool succeeded, double durationSeconds)
    {
        var tags = new TagList { { "outcome", succeeded ? "success" : "failure" } };
        ArchiveLoads.Add(1, tags);
        ArchiveLoadDuration.Record(durationSeconds, tags);
    }

    public static void RecordContainerDecode(CompressionType? compression, string outcome, double durationSeconds)
    {
        var compressionName = compression switch
        {
            CompressionType.None => "none",
            CompressionType.Gzip => "gzip",
            CompressionType.Bzip2 => "bzip2",
            _ => "unknown"
        };
        var tags = new TagList
        {
            { "compression", compressionName },
            { "outcome", outcome }
        };
        ContainerDecodeDuration.Record(durationSeconds, tags);
    }
}
