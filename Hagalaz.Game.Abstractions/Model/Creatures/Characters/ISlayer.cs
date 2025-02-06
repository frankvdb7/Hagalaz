namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISlayer
    {
        /// <summary>
        /// Gets the required count.
        /// </summary>
        /// <value>
        /// The required count.
        /// </value>
        int CurrentKillCount { get; }
        /// <summary>
        /// Gets the slayer master identifier.
        /// </summary>
        /// <value>
        /// The slayer master identifier.
        /// </value>
        int SlayerMasterId { get; }
        /// <summary>
        /// Gets the name of the task.
        /// </summary>
        /// <value>
        /// The name of the task.
        /// </value>
        string CurrentTaskName { get; }
        /// <summary>
        /// Assigns the new task.
        /// </summary>
        /// <param name="slayerMasterId">The slayer master identifier.</param>
        void AssignNewTask(int slayerMasterId);
    }
}
