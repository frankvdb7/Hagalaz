using Hagalaz.Game.Abstractions.Logic.Random;

namespace Hagalaz.Game.Abstractions.Services.Model
{
    public interface ISlayerTaskDefinition : IRandomObject
    {
        /// <summary>
        /// Gets the Id.
        /// </summary>
        /// <value>
        /// The Id.
        /// </value>
        public int Id { get; }
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; }

        /// <summary>
        /// Gets the NPC ids.
        /// </summary>
        /// <value>
        /// The NPC ids.
        /// </value>
        public int[] NpcIds { get; }

        /// <summary>
        /// Gets the level requirement.
        /// </summary>
        /// <value>
        /// The level requirement.
        /// </value>
        public int LevelRequirement { get; }

        /// <summary>
        /// Gets the combat level requirement.
        /// </summary>
        /// <value>
        /// The combat level requirement.
        /// </value>
        public int CombatLevelRequirement { get; }

        /// <summary>
        /// The mimimum loot.
        /// </summary>
        /// <value>The minimum count.</value>
        public int MinimumCount { get; }

        /// <summary>
        /// The maximum loot.
        /// </summary>
        /// <value>The maximum count.</value>
        public int MaximumCount { get; }

        /// <summary>
        /// Contains the coin count.
        /// </summary>
        public int CoinCount { get; }

        /// <summary>
        /// Gets the slayer master identifier.
        /// </summary>
        /// <value>
        /// The slayer master identifier.
        /// </value>
        public int SlayerMasterId { get; }
    }
}
