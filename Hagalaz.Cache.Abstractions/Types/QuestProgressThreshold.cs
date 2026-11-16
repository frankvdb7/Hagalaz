namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>A static cache row <c>[id, startedThreshold, completedThreshold]</c> for a quest progress varp or varbit.</summary>
    /// <param name="Id">The varp or varbit id.</param>
    /// <param name="StartedThreshold">The value from which the quest counts as started.</param>
    /// <param name="CompletedThreshold">The value from which the quest counts as completed.</param>
    public readonly record struct QuestProgressThreshold(int Id, int StartedThreshold, int CompletedThreshold);
}
