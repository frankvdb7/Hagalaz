namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>A varp or varbit value range required by a quest, with its description.</summary>
    /// <param name="Id">The varp or varbit id.</param>
    /// <param name="Minimum">The minimum accepted value.</param>
    /// <param name="Maximum">The maximum accepted value.</param>
    /// <param name="Description">The requirement description.</param>
    public readonly record struct QuestVariableRequirement(int Id, int Minimum, int Maximum, string Description);
}
