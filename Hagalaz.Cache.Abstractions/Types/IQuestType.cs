namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// Defines the contract for a quest type, representing the static data for a quest
    /// as loaded from the game cache.
    /// </summary>
    /// <remarks>
    /// This interface currently serves as a marker for quest-related types and inherits from <see cref="IType"/>.
    /// It can be extended with properties and methods specific to quests in the future.
    /// </remarks>
    public interface IQuestType : IType
    {
    }
}
