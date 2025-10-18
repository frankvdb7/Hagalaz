namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// Defines the primary completion status of a quest for a character.
    /// </summary>
    public enum QuestPrimaryStatus : byte
    {
        /// <summary>
        /// The character has not started the quest.
        /// </summary>
        NotStarted = 0,
        /// <summary>
        /// The character has started the quest but has not yet completed it.
        /// </summary>
        InComplete = 1,
        /// <summary>
        /// The character has completed the quest.
        /// </summary>
        Complete = 2,
    }
}