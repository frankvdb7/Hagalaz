using System;
using System.Diagnostics;

namespace Hagalaz.Services.GameWorld.Diagnostics;

internal static class GameObjectDefinitionResolutionDiagnostics
{
    private static readonly ActivitySource ActivitySource = new("Hagalaz.Services.GameWorld");

    public const string BulkResolutionActivityName = "Hagalaz.Services.GameWorld.GameObjectDefinitions.ResolveBulk";

    public static Activity? StartActivity() => ActivitySource.StartActivity(BulkResolutionActivityName, ActivityKind.Internal);

    public static void RecordFailure(Activity? activity, Exception exception)
    {
        if (activity is null)
        {
            return;
        }

        var cancelled = exception is OperationCanceledException;
        activity.SetTag("outcome", cancelled ? "cancelled" : "failure");
        activity.SetTag("error.type", exception.GetType().FullName);
        if (!cancelled)
        {
            activity.SetStatus(ActivityStatusCode.Error);
        }
    }
}
