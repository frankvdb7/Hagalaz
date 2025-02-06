using Hagalaz.Game.Abstractions.Logic.Skills;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Services.GameWorld.Logic.Skills
{
    /// <summary>
    /// 
    /// </summary>
    public class SlayerTaskDefinition : ISlayerObject, ISlayerTaskDefinition
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
        /// Contains the probability.
        /// </summary>
        public double Probability { get; }

        /// <summary>
        /// Gets the slayer master identifier.
        /// </summary>
        /// <value>
        /// The slayer master identifier.
        /// </value>
        public int SlayerMasterId { get; }

        /// <summary>
        /// Gets or sets whether this object will always be part of the result set
        /// (Probability is ignored when this flag is set to true)
        /// </summary>
        public bool Always { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="SlayerTaskDefinition" /> is enabled.
        /// Only enabled entries can be part of the result of a ILootTable.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SlayerTaskDefinition" /> class.
        /// </summary>
        /// <param name="id">The Id.</param>
        /// <param name="name">The name.</param>
        /// <param name="slayerMasterID">The slayer master Id.</param>
        /// <param name="npcIds">The NPC Ids.</param>
        /// <param name="levelRequirement">The level requirement.</param>
        /// <param name="combatLevelRequirement">The combat level requirement.</param>
        /// <param name="minimumCount">The minimum count.</param>
        /// <param name="maximumCount">The maximum count.</param>
        /// <param name="coinCount">The coin coint.</param>
        public SlayerTaskDefinition(int id, string name, int slayerMasterID, int[] npcIds, int levelRequirement, int combatLevelRequirement, int minimumCount, int maximumCount, int coinCount)
        {
            Id = id;
            SlayerMasterId = slayerMasterID;
            Name = name;
            NpcIds = npcIds;
            MinimumCount = minimumCount;
            MaximumCount = maximumCount;
            CoinCount = coinCount;
            LevelRequirement = levelRequirement;
            CombatLevelRequirement = combatLevelRequirement;
            Always = false;
            Enabled = true;
        }

        public double GetProbability(ICharacter character)
        {
            if (character.Statistics.GetSkillLevel(StatisticsConstants.Slayer) >= LevelRequirement && character.Statistics.FullCombatLevel >= CombatLevelRequirement)
                return 1.0;
            return 0.0; // no chance of getting this task.
        }
    }
}