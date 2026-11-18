using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Hagalaz.Game.Scripts.Skills;

internal static class SkillActivityMetrics
{
    private static readonly Meter Meter = new("Hagalaz.Services.GameWorld");
    private static readonly Counter<long> CompletedActions = Meter.CreateCounter<long>(
        "hagalaz.gameworld.skill.action.completed",
        "{action}");

    public static void RecordCookingCompletion() => RecordCompletion("cooking");

    public static void RecordFishingCompletion() => RecordCompletion("fishing");

    private static void RecordCompletion(string skill) => CompletedActions.Add(1, new TagList { { "skill", skill } });
}
