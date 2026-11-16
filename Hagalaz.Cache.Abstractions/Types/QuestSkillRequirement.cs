namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>A minimum skill level required by a quest.</summary>
    /// <param name="SkillId">The skill id.</param>
    /// <param name="Level">The minimum level.</param>
    public readonly record struct QuestSkillRequirement(int SkillId, int Level);
}
