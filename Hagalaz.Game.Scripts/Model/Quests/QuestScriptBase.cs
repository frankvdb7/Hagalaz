using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Model.Quests
{
    /// <summary>
    /// Contains the quest script.
    /// </summary>
    public abstract class QuestScriptBase
    {
        /// <summary>
        /// Contains quest instance.
        /// </summary>
        /// <value>The quest instance.</value>
        protected IQuest Quest { get; private set; }

        /// <summary>
        /// Contains character instance.
        /// </summary>
        /// <value>The character instance.</value>
        protected ICharacter Character { get; private set; }

        /// <summary>
        /// Initializes this script with given quest and character.
        /// </summary>
        /// <param name="quest">The quest.</param>
        /// <param name="character">The character.</param>
        public void Initialize(IQuest quest, ICharacter character)
        {
            Quest = quest;
            Character = character;

            // default event handlers
            //Quest.OnQuestPrimaryStatusChanged += UpdateQuestProgress;
            //Quest.OnQuestSecondaryStatusFlagChanged += UpdateQuestProgress;

            Initialize();
        }

        /// <summary>
        /// Starts the quest.
        /// </summary>
        /// <returns></returns>
        public virtual void StartQuest() => Quest.PrimaryStatus = QuestPrimaryStatus.InComplete;

        /// <summary>
        /// Updates the progress.
        /// </summary>
        public virtual void UpdateQuestProgress() { }

        /// <summary>
        /// Ends the quest.
        /// </summary>
        /// <returns></returns>
        public virtual void EndQuest()
        {
            if (CanEndQuest()) Quest.PrimaryStatus = QuestPrimaryStatus.Complete;
        }

        /// <summary>
        /// Determines whether this instance [can start quest] the specified character.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance [can start quest] the specified character; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool CanStartQuest()
        {
            if (Quest.PrimaryStatus != QuestPrimaryStatus.NotStarted)
                return false;
            // TODO 2023/07/30
            //for (int i = 0; i < Quest.Definition.RequiredQuestIds.Length; i++)
            //    if (Character.Quests.GetQuestStatus(Quest.Definition.RequiredQuestIds[i]) != QuestPrimaryStatus.Complete)
            //        return false;

            return true;
        }

        /// <summary>
        /// Determines whether this instance [can end quest].
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance [can end quest]; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool CanEndQuest()
        {
            if (Quest.PrimaryStatus != QuestPrimaryStatus.Complete)
                return false;
            return true;
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        protected abstract void Initialize();

        /// <summary>
        /// Gets the quest id.
        /// </summary>
        /// <returns>The quest id this script is bound to.</returns>
        public abstract int GetQuestId();
    }
}