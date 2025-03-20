using Hagalaz.Game.Abstractions.Logic.Skills;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Services.GameWorld.Logic.Skills
{
    /// <summary>
    /// 
    /// </summary>
    public record SlayerTaskDefinition : ISlayerObject, ISlayerTaskDefinition
    {
        /// <summary>
        /// Gets the Id.
        /// </summary>
        /// <value>
        /// The Id.
        /// </value>
        public required int Id { get; init;}

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public required string Name { get; init; }

        /// <summary>
        /// Gets the NPC ids.
        /// </summary>
        /// <value>
        /// The NPC ids.
        /// </value>
        public required int[] NpcIds { get; init; }

        /// <summary>
        /// Gets the level requirement.
        /// </summary>
        /// <value>
        /// The level requirement.
        /// </value>
        public required int LevelRequirement { get; init; }

        /// <summary>
        /// Gets the combat level requirement.
        /// </summary>
        /// <value>
        /// The combat level requirement.
        /// </value>
        public required int CombatLevelRequirement { get; init; }

        /// <summary>
        /// The mimimum loot.
        /// </summary>
        /// <value>The minimum count.</value>
        public required int MinimumCount { get; init; }

        /// <summary>
        /// The maximum loot.
        /// </summary>
        /// <value>The maximum count.</value>
        public required int MaximumCount { get; init; }

        /// <summary>
        /// Contains the coin count.
        /// </summary>
        public required int CoinCount { get; init; }

        /// <summary>
        /// Contains the probability.
        /// </summary>
        public required double Probability { get; init; }

        /// <summary>
        /// Gets the slayer master identifier.
        /// </summary>
        /// <value>
        /// The slayer master identifier.
        /// </value>
        public required int SlayerMasterId { get; init; }

        /// <summary>
        /// Gets or sets whether this object will always be part of the result set
        /// (Probability is ignored when this flag is set to true)
        /// </summary>
        public required bool Always { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="SlayerTaskDefinition" /> is enabled.
        /// Only enabled entries can be part of the result of a ILootTable.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; } = true;
    }
}