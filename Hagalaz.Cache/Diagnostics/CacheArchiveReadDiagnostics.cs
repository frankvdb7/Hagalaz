using System;
using System.Diagnostics;
using Hagalaz.Cache.Abstractions.Model;

namespace Hagalaz.Cache.Diagnostics;

internal static class CacheArchiveReadDiagnostics
{
    private static readonly ActivitySource ActivitySource = new("Hagalaz.Cache");

    private const string ReadArchiveActivityName = "Hagalaz.Cache.ReadArchive";

    public static Activity? StartReadArchive(int indexId, int archiveId)
    {
        var activity = ActivitySource.StartActivity(ReadArchiveActivityName, ActivityKind.Internal);
        activity?.SetTag("cache.index_id", indexId);
        activity?.SetTag("cache.archive_id", archiveId);
        activity?.SetTag("outcome", "started");
        return activity;
    }

    public static Activity? CurrentReadArchiveActivity
    {
        get
        {
            var activity = Activity.Current;
            return activity is { IsAllDataRequested: true } &&
                   activity.Source.Name == ActivitySource.Name &&
                   activity.OperationName == ReadArchiveActivityName
                ? activity
                : null;
        }
    }

    public static Activity? CurrentReadArchiveActivityForFile(int indexId, int archiveId)
    {
        var activity = CurrentReadArchiveActivity;
        if (activity is null)
        {
            return null;
        }

        return activity.GetTagItem("cache.index_id") is int activityIndexId && activityIndexId == indexId &&
               activity.GetTagItem("cache.archive_id") is int activityArchiveId && activityArchiveId == archiveId
            ? activity
            : null;
    }

    public static long StartTiming(Activity? activity) => activity is { IsAllDataRequested: true }
        ? Stopwatch.GetTimestamp()
        : 0;

    public static void RecordElapsed(Activity? activity, string tag, long startTimestamp)
    {
        if (startTimestamp == 0 || activity is not { IsAllDataRequested: true })
        {
            return;
        }

        activity.SetTag(tag, Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds);
    }

    public static void RecordContainer(Activity? activity, long rawBytes, IContainer container)
    {
        if (activity is not { IsAllDataRequested: true })
        {
            return;
        }

        var headerBytes = container.CompressionType == CompressionType.None ? 5 : 9;
        var trailerBytes = container.IsVersioned() ? 2 : 0;
        var compressedBytes = Math.Max(0, rawBytes - headerBytes - trailerBytes);
        var compression = container.CompressionType switch
        {
            CompressionType.None => "none",
            CompressionType.Gzip => "gzip",
            CompressionType.Bzip2 => "bzip2",
            _ => "unknown"
        };

        activity.SetTag("container.compression", compression);
        activity.SetTag("container.compressed_bytes", compressedBytes);
        activity.SetTag("container.expanded_bytes", container.Data.Length);
    }

    public static void RecordArchive(Activity? activity, int memberCount, long payloadBytes)
    {
        if (activity is not { IsAllDataRequested: true })
        {
            return;
        }

        activity.SetTag("archive.member_count", memberCount);
        activity.SetTag("archive.payload_bytes", payloadBytes);
    }

    public static void RecordFailure(Activity? activity, Exception exception)
    {
        if (activity is null)
        {
            return;
        }

        var cancelled = exception is OperationCanceledException;
        activity.SetTag("outcome", cancelled ? "cancelled" : "error");
        if (!cancelled)
        {
            activity.SetStatus(ActivityStatusCode.Error);
        }
    }
}
