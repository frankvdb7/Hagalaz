namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the contract for managing a character's Slayer skill, including their current task and kill count.
    /// </summary>
    public interface ISlayer
    {
        /// <summary>
        /// Gets the number of creatures the character has killed for their current Slayer task.
        /// </summary>
        int CurrentKillCount { get; }
        /// <summary>
        /// Gets the ID of the character's current Slayer task.
        /// </summary>
        int CurrentTaskId { get; }
        /// <summary>
        /// Assigns a new Slayer task to the character from a specific Slayer Master.
        /// </summary>
        /// <param name="slayerMasterId">The ID of the Slayer Master assigning the task.</param>
        void AssignNewTask(int slayerMasterId);
    }
}
