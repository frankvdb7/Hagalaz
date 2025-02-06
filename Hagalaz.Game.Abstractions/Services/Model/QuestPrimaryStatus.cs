namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// Contains the quest status.
    /// </summary>
    public enum QuestPrimaryStatus : byte
    {
        /// <summary>
        /// The character has not started the quest.
        /// </summary>
        NotStarted = 0,

        /// <summary>
        /// The character has started the quest.
        /// </summary>
        InComplete = 1,

        /// <summary>
        /// The character has completed the quest.
        /// </summary>
        Complete = 2,
    }
}