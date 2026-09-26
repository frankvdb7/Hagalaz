using System;
using System.Diagnostics;

namespace Hagalaz.Services.GameWorld.Diagnostics;

internal static class GameObjectDefinitionResolutionDiagnostics
{
    private static readonly ActivitySource ActivitySource = new("Hagalaz.Services.GameWorld");

    public const string BulkResolutionActivityName = "Hagalaz.Services.GameWorld.GameObjectDefinitions.ResolveBulk";
    public const string CacheLookupActivityName = "Hagalaz.Services.GameWorld.GameObjectDefinitions.CacheLookup";
    public const string CompositionActivityName = "Hagalaz.Services.GameWorld.GameObjectDefinitions.Compose";
    public const string RepositoryActivityName = "Hagalaz.Services.GameWorld.GameObjectDefinitions.Repository";

    public static Activity? StartActivity(string name) => ActivitySource.StartActivity(name, ActivityKind.Internal);

    public static Activity? CurrentCompositionActivity
    {
        get
        {
            var activity = Activity.Current;
            return activity is { IsAllDataRequested: true } && activity.OperationName == CompositionActivityName
                ? activity
                : null;
        }
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

        AddDuration(activity, tag, Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds);
    }

    public static void AddDuration(Activity? activity, string tag, double durationMilliseconds)
    {
        if (activity is not { IsAllDataRequested: true })
        {
            return;
        }

        var current = activity.GetTagItem(tag) is double value ? value : 0d;
        activity.SetTag(tag, current + durationMilliseconds);
    }

    public static void AddCount(Activity? activity, string tag, int count = 1)
    {
        if (activity is not { IsAllDataRequested: true })
        {
            return;
        }

        var current = activity.GetTagItem(tag) is int value ? value : 0;
        activity.SetTag(tag, current + count);
    }

    public static void RecordMissingArchiveDefinition()
    {
        AddCount(CurrentCompositionActivity, "archive.missing_definition_count");
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
