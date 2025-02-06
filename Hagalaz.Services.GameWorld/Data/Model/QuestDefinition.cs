using Hagalaz.Cache.Types;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Services.GameWorld.Data.Model
{
    /// <summary>
    /// Contains a quest definition.
    /// </summary>
    public class QuestDefinition : QuestType, IQuestDefinition
    {
        /// <summary>
        /// Contains the count of quest objectives.
        /// </summary>
        public const int QuestObjectivesCount = 4;

        /// <summary>
        /// Contains the count of quest rewards.
        /// </summary>
        public const int QuestRewardsCount = 4;

        /// <summary>
        /// Contains the required quest ids.
        /// </summary>
        public short[] RequiredQuestIds { get; }

        /// <summary>
        /// Contains the required skill ids.
        /// </summary>
        public byte[] RequiredSkillIds { get; }

        /// <summary>
        /// Contains the required skill levels.
        /// </summary>
        public byte[] RequiredSkillLevels { get; }

        /// <summary>
        /// Contains the required item ids.
        /// </summary>
        public short[] RequiredItemIds { get; }

        /// <summary>
        /// Contains the required item counts.
        /// </summary>
        public int[] RequiredItemCounts { get; }

        /// <summary>
        /// Contains the required NPC ids.
        /// </summary>
        public short[] RequiredNpcIds { get; }

        /// <summary>
        /// Contains the required NPC counts.
        /// </summary>
        public int[] RequiredNpcCounts { get; }

        /// <summary>
        /// Contains the reward skill ids.
        /// </summary>
        public byte[] RewardSkillIds { get; }

        /// <summary>
        /// Contains the reward skill experience.
        /// </summary>
        public int[] RewardSkillExperience { get; }

        /// <summary>
        /// Contains the reward item ids.
        /// </summary>
        public short[] RewardItemIds { get; }

        /// <summary>
        /// Contains the reward item counts.
        /// </summary>
        public int[] RewardItemCounts { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuestDefinition" /> class.
        /// </summary>
        /// <param name="questId">The quest id.</param>
        public QuestDefinition(int questId)
            : base(questId)
        {
            RequiredQuestIds = new short[QuestObjectivesCount];
            RequiredSkillIds = new byte[QuestObjectivesCount];
            RequiredSkillLevels = new byte[QuestObjectivesCount];
            RequiredItemIds = new short[QuestObjectivesCount];
            RequiredItemCounts = new int[QuestObjectivesCount];
            RequiredNpcIds = new short[QuestObjectivesCount];
            RequiredNpcCounts = new int[QuestObjectivesCount];
            RewardSkillIds = new byte[QuestRewardsCount];
            RewardSkillExperience = new int[QuestRewardsCount];
            RewardItemIds = new short[QuestRewardsCount];
            RewardItemCounts = new int[QuestRewardsCount];
        }
    }
}