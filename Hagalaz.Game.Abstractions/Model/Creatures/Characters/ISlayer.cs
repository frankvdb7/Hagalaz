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
        /// Gets the current task id.
        /// </summary>
        int CurrentTaskId { get; }
        /// <summary>
        /// Assigns the new task.
        /// </summary>
        /// <param name="slayerMasterId">The slayer master identifier.</param>
        void AssignNewTask(int slayerMasterId);
    }
}
