using Hagalaz.Game.Abstractions.Logic.Random;

namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// Defines the contract for a Slayer task definition.
    /// </summary>
    public interface ISlayerTaskDefinition : IRandomObject
    {
        /// <summary>
        /// Gets the unique identifier for the task.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the name of the task (e.g., "Slay 50 Kalphites").
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the array of NPC IDs that count towards this task.
        /// </summary>
        public int[] NpcIds { get; }

        /// <summary>
        /// Gets the required Slayer level to receive this task.
        /// </summary>
        public int LevelRequirement { get; }

        /// <summary>
        /// Gets the required combat level to receive this task.
        /// </summary>
        public int CombatLevelRequirement { get; }

        /// <summary>
        /// Gets the minimum number of creatures assigned for this task.
        /// </summary>
        public int MinimumCount { get; }

        /// <summary>
        /// Gets the maximum number of creatures assigned for this task.
        /// </summary>
        public int MaximumCount { get; }

        /// <summary>
        /// Gets the number of coins awarded upon completing this task.
        /// </summary>
        public int CoinCount { get; }

        /// <summary>
        /// Gets the ID of the Slayer master who can assign this task.
        /// </summary>
        public int SlayerMasterId { get; }
    }
}