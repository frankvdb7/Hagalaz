using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Quests;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Utilities;

namespace Hagalaz.Services.GameWorld.Services.Model
{
    /// <summary>
    /// Represents a quest that is currently active on a character.
    /// </summary>
    public class Quest : IQuest
    {
        /// <summary>
        /// The primary status.
        /// </summary>
        private QuestPrimaryStatus _primaryStatus;

        /// <summary>
        /// The secondary status flag.
        /// </summary>
        private int _secondaryStatusFlag;

        /// <summary>
        /// Occurs when [quest primary status changed].
        /// </summary>
        public event Action OnQuestPrimaryStatusChanged;

        /// <summary>
        /// Occurs when [quest secondary status flag changed].
        /// </summary>
        public event Action OnQuestSecondaryStatusFlagChanged;

        /// <summary>
        /// Contains the quest owner.
        /// </summary>
        public ICharacter Owner { get; }

        /// <summary>
        /// Contains the quest script.
        /// </summary>
        public IQuestScript Script { get; }

        /// <summary>
        /// Contains the quest definition.
        /// </summary>
        public IQuestDefinition Definition { get; }

        /// <summary>
        /// Contains the quest status.
        /// </summary>
        public QuestPrimaryStatus PrimaryStatus
        {
            get => _primaryStatus;
            set
            {
                if (SetPropertyUtility.SetStruct(ref _primaryStatus, value))
                    OnQuestPrimaryStatusChanged?.Invoke();
            }
        }

        /// <summary>
        /// Contains the secondary status.
        /// </summary>
        public int SecondaryStatusFlag => _secondaryStatusFlag;

        public string Name => Definition.Name;

        /// <summary>
        /// Contains the quest stage.
        /// </summary>
        public short Stage { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Quest" /> class.
        /// </summary>
        /// <param name="questId">The quest id.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="status">The status.</param>
        /// <param name="stage">The stage.</param>
        /// <param name="script">The script.</param>
        public Quest(short questId, ICharacter owner, QuestPrimaryStatus status, short stage, IQuestScript? script = null)
        {
            Owner = owner;
            _primaryStatus = status;
            Stage = stage;
            //Definition = World.ContentManager.Quests.GetQuestDefinition(questId);
            //if (script == null)
            //Script = World.ContentManager.Quests.MakeQuestScript(this);
            //else
            // Script = script;
        }

        /// <summary>
        /// Adds the secondary status.
        /// </summary>
        /// <param name="statusFlag">The status flag.</param>
        public void AddSecondaryStatus(int statusFlag)
        {
            if ((_secondaryStatusFlag & statusFlag) == 0)
            {
                _secondaryStatusFlag |= statusFlag;
                OnQuestSecondaryStatusFlagChanged?.Invoke();
            }
        }

        /// <summary>
        /// Removes the secondary status.
        /// </summary>
        /// <param name="statusFlag">The status flag.</param>
        public void RemoveSecondaryStatus(int statusFlag)
        {
            if ((_secondaryStatusFlag & statusFlag) != 0)
            {
                _secondaryStatusFlag &= ~statusFlag;
                OnQuestSecondaryStatusFlagChanged?.Invoke();
            }
        }
    }
}