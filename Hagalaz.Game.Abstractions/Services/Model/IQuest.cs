namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// Defines the contract for a quest's state for a specific player.
    /// </summary>
    public interface IQuest
    {
        /// <summary>
        /// Gets the name of the quest.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets or sets the primary status of the quest (e.g., NotStarted, InProgress, Completed).
        /// </summary>
        QuestPrimaryStatus PrimaryStatus { get; set; }
    }
}